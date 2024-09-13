#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;
using Timer = System.Timers.Timer;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006, IDE0018, CA2000, CA1031, CS1634, CS1570, CS1696, CA1836, CA1854, CA1834, CA2201, CA1725, CA1861, CA1024

namespace Speckle.Core.Transports;

/// <summary>
/// Sends data to a speckle server.
/// </summary>
[Obsolete("Use " + nameof(ServerTransport))]
public sealed class ServerTransportV1 : IDisposable, ICloneable, ITransport
{
  private const int DownloadBatchSize = 1000;

  private bool _isWriting;

  private const int MaxBufferSize = 1_000_000;

  private const int MaxMultipartCount = 50;

  private ConcurrentQueue<(string, string, int)> _queue = new();

  private int _totalElapsed;

  private const int PollInterval = 100;

  private Timer _writeTimer;

  public ServerTransportV1(Account account, string streamId, int timeoutSeconds = 60)
  {
    Account = account;
    Initialize(account.serverInfo.url, streamId, account.token, timeoutSeconds);
  }

  public string BaseUri { get; private set; }

  public string StreamId { get; set; }

  private HttpClient Client { get; set; }

  public bool CompressPayloads { get; set; } = true;

  public int TotalSentBytes { get; set; }

  public Account Account { get; set; }

  public object Clone()
  {
    return new ServerTransport(Account, StreamId)
    {
      OnErrorAction = OnErrorAction,
      OnProgressAction = OnProgressAction,
      CancellationToken = CancellationToken
    };
  }

  public void Dispose()
  {
    // TODO: check if it's writing first?
    Client?.Dispose();
    _writeTimer.Dispose();
  }

  public string TransportName { get; set; } = "RemoteTransport";

  public Dictionary<string, object> TransportContext =>
    new()
    {
      { "name", TransportName },
      { "type", GetType().Name },
      { "streamId", StreamId },
      { "serverUrl", BaseUri }
    };

  public CancellationToken CancellationToken { get; set; }

  public int SavedObjectCount { get; private set; }

  public Action<string, int> OnProgressAction { get; set; }

  public Action<string, Exception> OnErrorAction { get; set; }

  // not implementing this for V1, just a dummy 0 value
  public TimeSpan Elapsed => TimeSpan.Zero;

  public void BeginWrite()
  {
    if (!GetWriteCompletionStatus())
    {
      throw new SpeckleException("Transport is still writing.");
    }

    TotalSentBytes = 0;
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public async Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
  {
    var payload = new Dictionary<string, string> { { "objects", JsonConvert.SerializeObject(objectIds) } };
    var uri = new Uri($"/api/diff/{StreamId}", UriKind.Relative);
    var response = await Client
      .PostAsync(
        uri,
        new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
        CancellationToken
      )
      .ConfigureAwait(false);
    response.EnsureSuccessStatusCode();

    var hasObjectsJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    var hasObjects = JsonConvert.DeserializeObject<Dictionary<string, bool>>(hasObjectsJson);
    return hasObjects;
  }

  private void Initialize(string baseUri, string streamId, string authorizationToken, int timeoutSeconds = 60)
  {
    SpeckleLog.Logger.Information("Initializing New Remote V1 Transport for {baseUri}", baseUri);

    BaseUri = baseUri;
    StreamId = streamId;

    Client = Http.GetHttpProxyClient(
      new SpeckleHttpClientHandler(Http.HttpAsyncPolicy()) { AutomaticDecompression = DecompressionMethods.GZip }
    );

    Client.BaseAddress = new Uri(baseUri);
    Client.Timeout = new TimeSpan(0, 0, timeoutSeconds);
    Http.AddAuthHeader(Client, authorizationToken);

    _writeTimer = new Timer
    {
      AutoReset = true,
      Enabled = false,
      Interval = PollInterval
    };
    _writeTimer.Elapsed += WriteTimerElapsed;
  }

  public override string ToString()
  {
    return $"Server Transport @{Account.serverInfo.url}";
  }

  internal class Placeholder
  {
    public Dictionary<string, int> __closure { get; set; } = new();
  }

  #region Writing objects

  public async Task WriteComplete()
  {
    await Utilities
      .WaitUntil(
        () =>
        {
          return GetWriteCompletionStatus();
        },
        50
      )
      .ConfigureAwait(false);
  }

  public bool GetWriteCompletionStatus()
  {
    return _queue.Count == 0 && !_isWriting;
  }

  private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
  {
    _totalElapsed += PollInterval;

    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      _isWriting = false;
      return;
    }

    if (_totalElapsed > 300 && !_isWriting && _queue.Count != 0)
    {
      _totalElapsed = 0;
      _writeTimer.Enabled = false;
#pragma warning disable CS4014
      ConsumeQueue();
#pragma warning restore CS4014
    }
  }

  /// <summary>
  /// Consumes a batch of objects from Queue, of MAX_BUFFER_SIZE or until queue is empty, and filters out the objects that already exist on the server
  /// </summary>
  /// <returns>
  /// Tuple of:
  ///  - int: the number of objects consumed from the queue (useful to report progress)
  ///  - List<(string, string, int)>: List of queued objects that are not already on the server
  /// </returns>
  private async Task<(int, List<(string, string, int)>)> ConsumeNewBatch()
  {
    // Read a batch from the queue

    List<(string, string, int)> queuedBatch = new();
    List<string> queuedBatchIds = new();
    ValueTuple<string, string, int> queueElement;
    var payloadBufferSize = 0;
    while (_queue.TryPeek(out queueElement) && payloadBufferSize < MaxBufferSize)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return (queuedBatch.Count, null);
      }

      _queue.TryDequeue(out queueElement);
      queuedBatch.Add(queueElement);
      queuedBatchIds.Add(queueElement.Item1);
      payloadBufferSize += queueElement.Item3;
    }

    // Ask the server which objects from the batch it already has
    Dictionary<string, bool> hasObjects = null;
    try
    {
      hasObjects = await HasObjects(queuedBatchIds).ConfigureAwait(false);
    }
    catch (Exception e)
    {
      OnErrorAction?.Invoke(TransportName, e);
      return (queuedBatch.Count, null);
    }

    // Filter the queued batch to only return new objects

    List<(string, string, int)> newBatch = new();
    foreach (var queuedItem in queuedBatch)
    {
      if (!hasObjects.ContainsKey(queuedItem.Item1) || !hasObjects[queuedItem.Item1])
      {
        newBatch.Add(queuedItem);
      }
    }

    return (queuedBatch.Count, newBatch);
  }

  private async Task ConsumeQueue()
  {
    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      _isWriting = false;
      return;
    }

    if (_queue.Count == 0)
    {
      return;
    }

    _isWriting = true;
    using var message = new HttpRequestMessage
    {
      RequestUri = new Uri($"/objects/{StreamId}", UriKind.Relative),
      Method = HttpMethod.Post
    };

    using var multipart = new MultipartFormDataContent("--obj--");

    SavedObjectCount = 0;
    var addedMpCount = 0;

    while (addedMpCount < MaxMultipartCount && _queue.Count != 0)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        _queue = new ConcurrentQueue<(string, string, int)>();
        _isWriting = false;
        return;
      }

      (int consumedQueuedObjects, List<(string, string, int)> batch) = await ConsumeNewBatch().ConfigureAwait(false);
      if (batch == null)
      {
        // Canceled or error happened (which was already reported)
        _queue = new ConcurrentQueue<(string, string, int)>();
        _isWriting = false;
        return;
      }

      if (batch.Count == 0)
      {
        // The server already has all objects from the queued batch
        SavedObjectCount += consumedQueuedObjects;
        continue;
      }

      var _ctBuilder = new StringBuilder("[");
      for (int i = 0; i < batch.Count; i++)
      {
        if (i > 0)
        {
          _ctBuilder.Append(",");
        }

        _ctBuilder.Append(batch[i].Item2);
        TotalSentBytes += batch[i].Item3;
      }
      _ctBuilder.Append("]");
      string _ct = _ctBuilder.ToString();

      if (CompressPayloads)
      {
        var content = new GzipContent(new StringContent(_ct, Encoding.UTF8));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
        multipart.Add(content, $"batch-{addedMpCount}", $"batch-{addedMpCount}");
      }
      else
      {
        multipart.Add(new StringContent(_ct, Encoding.UTF8), $"batch-{addedMpCount}", $"batch-{addedMpCount}");
      }

      addedMpCount++;
      SavedObjectCount += consumedQueuedObjects;
    }

    message.Content = multipart;

    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      _isWriting = false;
      return;
    }

    if (addedMpCount > 0)
    {
      try
      {
        var response = await Client.SendAsync(message, CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
      }
      catch (Exception e)
      {
        _isWriting = false;
        OnErrorAction?.Invoke(
          TransportName,
          new Exception($"Remote error: {Account.serverInfo.url} is not reachable. \n {e.Message}", e)
        );

        _queue = new ConcurrentQueue<(string, string, int)>();
        return;
      }
    }

    _isWriting = false;

    OnProgressAction?.Invoke(TransportName, SavedObjectCount);

    if (!_writeTimer.Enabled)
    {
      _writeTimer.Enabled = true;
      _writeTimer.Start();
    }
  }

  public void SaveObject(string hash, string serializedObject)
  {
    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      _isWriting = false;
      return;
    }

    _queue.Enqueue((hash, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

    if (!_writeTimer.Enabled && !_isWriting)
    {
      _writeTimer.Enabled = true;
      _writeTimer.Start();
    }
  }

  public void SaveObject(string hash, ITransport sourceTransport)
  {
    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      _isWriting = false;
      return;
    }

    var serializedObject = sourceTransport.GetObject(hash);

    _queue.Enqueue((hash, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

    if (!_writeTimer.Enabled && !_isWriting)
    {
      _writeTimer.Enabled = true;
      _writeTimer.Start();
    }
  }

  #endregion

  #region Getting objects

  public string GetObject(string id)
  {
    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      return null;
    }

    using var message = new HttpRequestMessage
    {
      RequestUri = new Uri($"/objects/{StreamId}/{id}/single", UriKind.Relative),
      Method = HttpMethod.Get
    };

    var response = Client
      .SendAsync(message, HttpCompletionOption.ResponseContentRead, CancellationToken)
      .Result.Content;
    return response.ReadAsStringAsync().Result;
  }

  public async Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int> onTotalChildrenCountKnown
  )
  {
    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      return null;
    }

    // Get root object
    using var rootHttpMessage = new HttpRequestMessage
    {
      RequestUri = new Uri($"/objects/{StreamId}/{id}/single", UriKind.Relative),
      Method = HttpMethod.Get
    };

    HttpResponseMessage rootHttpResponse = null;
    try
    {
      rootHttpResponse = await Client
        .SendAsync(rootHttpMessage, HttpCompletionOption.ResponseContentRead, CancellationToken)
        .ConfigureAwait(false);
      rootHttpResponse.EnsureSuccessStatusCode();
    }
    catch (Exception e)
    {
      OnErrorAction?.Invoke(TransportName, e);
      return null;
    }

    string rootObjectStr = await rootHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
    List<string> childrenIds = new();
    var rootPartial = JsonConvert.DeserializeObject<Placeholder>(rootObjectStr);
    if (rootPartial.__closure != null)
    {
      childrenIds = new List<string>(rootPartial.__closure.Keys);
    }

    onTotalChildrenCountKnown?.Invoke(childrenIds.Count);

    var childrenFoundMap = await targetTransport.HasObjects(childrenIds).ConfigureAwait(false);
    List<string> newChildrenIds = new(from objId in childrenFoundMap.Keys where !childrenFoundMap[objId] select objId);

    targetTransport.BeginWrite();

    // Get the children that are not already in the targetTransport
    List<string> childrenIdBatch = new(DownloadBatchSize);
    bool downloadBatchResult;
    foreach (var objectId in newChildrenIds)
    {
      childrenIdBatch.Add(objectId);
      if (childrenIdBatch.Count >= DownloadBatchSize)
      {
        downloadBatchResult = await CopyObjects(childrenIdBatch, targetTransport).ConfigureAwait(false);
        if (!downloadBatchResult)
        {
          return null;
        }

        childrenIdBatch = new List<string>(DownloadBatchSize);
      }
    }
    if (childrenIdBatch.Count > 0)
    {
      downloadBatchResult = await CopyObjects(childrenIdBatch, targetTransport).ConfigureAwait(false);
      if (!downloadBatchResult)
      {
        return null;
      }
    }

    targetTransport.SaveObject(id, rootObjectStr);
    await targetTransport.WriteComplete().ConfigureAwait(false);
    return rootObjectStr;
  }

  private async Task<bool> CopyObjects(List<string> hashes, ITransport targetTransport)
  {
    Stream childrenStream = null;

    if (hashes.Count <= 0)
    {
      childrenStream = new MemoryStream();
    }
    else
    {
      using var childrenHttpMessage = new HttpRequestMessage
      {
        RequestUri = new Uri($"/api/getobjects/{StreamId}", UriKind.Relative),
        Method = HttpMethod.Post
      };

      Dictionary<string, string> postParameters = new();
      postParameters.Add("objects", JsonConvert.SerializeObject(hashes));
      childrenHttpMessage.Content = new FormUrlEncodedContent(postParameters);
      childrenHttpMessage.Headers.Add("Accept", "text/plain");

      HttpResponseMessage childrenHttpResponse = null;
      try
      {
        childrenHttpResponse = await Client
          .SendAsync(childrenHttpMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken)
          .ConfigureAwait(false);
        childrenHttpResponse.EnsureSuccessStatusCode();
      }
      catch (Exception e)
      {
        OnErrorAction?.Invoke(TransportName, e);
        return false;
      }

      childrenStream = await childrenHttpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    using var stream = childrenStream;
    using var reader = new StreamReader(stream, Encoding.UTF8);

    string line;
    while ((line = reader.ReadLine()) != null)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        _queue = new ConcurrentQueue<(string, string, int)>();
        return false;
      }

      var pcs = line.Split(new[] { '\t' }, 2);
      targetTransport.SaveObject(pcs[0], pcs[1]);

      OnProgressAction?.Invoke(TransportName, 1); // possibly make this more friendly
    }

    return true;
  }

  #endregion
}

/// <summary>
/// https://cymbeline.ch/2014/03/16/gzip-encoding-an-http-post-request-body/
/// </summary>
[Obsolete("Use " + nameof(ServerUtils.GzipContent))]
internal sealed class GzipContent : HttpContent
{
  private readonly HttpContent _content;

  public GzipContent(HttpContent content)
  {
    this._content = content;

    // Keep the original content's headers ...
    if (content != null)
    {
      foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
      {
        Headers.TryAddWithoutValidation(header.Key, header.Value);
      }
    }

    // ... and let the server know we've Gzip-compressed the body of this request.
    Headers.ContentEncoding.Add("gzip");
  }

  protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
  {
    // Open a GZipStream that writes to the specified output stream.
    using GZipStream gzip = new(stream, CompressionMode.Compress, true);
    if (_content != null)
    {
      await _content.CopyToAsync(gzip).ConfigureAwait(false);
    }
    else
    {
      await new StringContent(string.Empty).CopyToAsync(gzip).ConfigureAwait(false);
    }
  }

  protected override bool TryComputeLength(out long length)
  {
    length = -1;
    return false;
  }
}
#pragma warning restore IDE1006, IDE0018, CA2000, CA1031, CS1634, CS1570, CS1696, CA1836, CA1854, CA1834, CA2201, CA1725, CA1861, CA1024
