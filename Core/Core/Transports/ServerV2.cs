#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports.ServerUtils;
using Speckle.Newtonsoft.Json.Linq;

namespace Speckle.Core.Transports;

public class ServerTransport : ServerTransportV2
{
  public ServerTransport(Account account, string streamId, int timeoutSeconds = 60, string? blobStorageFolder = null)
    : base(account, streamId, timeoutSeconds, blobStorageFolder) { }
}

public class ServerTransportV2 : IDisposable, ICloneable, ITransport, IBlobCapableTransport
{
  private readonly object _elapsedLock = new();

  private Exception? _exception;
  private bool IsInErrorState => _exception is not null;
  private bool _isWriteComplete;

  // TODO: make send buffer more flexible to accept blobs too
  private List<(string id, string data)> _sendBuffer = new();
  private readonly object _sendBufferLock = new();
  private Thread? _sendingThread;

  private bool _shouldSendThreadRun;

  public ServerTransportV2(Account account, string streamId, int timeoutSeconds = 60, string? blobStorageFolder = null)
  {
    Account = account;
    Initialize(account.serverInfo.url, streamId, account.token, timeoutSeconds);
    BlobStorageFolder = blobStorageFolder ?? SpecklePathProvider.BlobStoragePath();

    Directory.CreateDirectory(BlobStorageFolder);
  }

  public int TotalSentBytes { get; private set; }

  public Account Account { get; set; }
  public string BaseUri { get; private set; }
  public string StreamId { get; set; }

  public int TimeoutSeconds { get; set; }
  private string AuthorizationToken { get; set; }

  internal ParallelServerApi Api { get; private set; }

  public string BlobStorageFolder { get; set; }

  public void SaveBlob(Blob obj)
  {
    if (string.IsNullOrEmpty(StreamId))
      throw new InvalidOperationException($"Invalid StreamID {StreamId}");
    var hash = obj.GetFileHash();

    lock (_sendBufferLock)
    {
      if (IsInErrorState)
        return;
      _sendBuffer.Add(($"blob:{hash}", obj.filePath));
    }
  }

  public object Clone()
  {
    return new ServerTransportV2(Account, StreamId, TimeoutSeconds, BlobStorageFolder)
    {
      OnErrorAction = OnErrorAction,
      OnProgressAction = OnProgressAction,
      CancellationToken = CancellationToken,
    };
  }

  public void Dispose()
  {
    if (_sendingThread != null)
    {
      _shouldSendThreadRun = false;
      _sendingThread.Join();
    }
    Api.Dispose();
  }

  public string TransportName { get; set; } = "RemoteTransport";

  public Dictionary<string, object> TransportContext =>
    new()
    {
      { "name", TransportName },
      { "type", GetType().Name },
      { "streamId", StreamId },
      { "serverUrl", BaseUri },
      { "blobStorageFolder", BlobStorageFolder }
    };

  public CancellationToken CancellationToken { get; set; }
  public Action<string, int>? OnProgressAction { get; set; }
  public Action<string, Exception>? OnErrorAction { get; set; }
  public int SavedObjectCount { get; private set; }
  public TimeSpan Elapsed { get; private set; } = TimeSpan.Zero;

  public async Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int>? onTotalChildrenCountKnown = null
  )
  {
    if (string.IsNullOrEmpty(StreamId) || string.IsNullOrEmpty(id) || targetTransport == null)
      throw new InvalidOperationException("Invalid parameters to CopyObjectAndChildren");

    CancellationToken.ThrowIfCancellationRequested();

    using ParallelServerApi api = new(BaseUri, AuthorizationToken, BlobStorageFolder, TimeoutSeconds);

    var stopwatch = Stopwatch.StartNew();
    api.CancellationToken = CancellationToken;

    string rootObjectJson = await api.DownloadSingleObject(StreamId, id).ConfigureAwait(false);
    List<string> allIds = ParseChildrenIds(rootObjectJson);

    List<string> childrenIds = allIds.Where(x => !x.Contains("blob:")).ToList();
    List<string> blobIds = allIds.Where(x => x.Contains("blob:")).Select(x => x.Remove(0, 5)).ToList();

    onTotalChildrenCountKnown?.Invoke(allIds.Count);

    //
    // Objects download
    //

    // Check which children are not already in the local transport
    var childrenFoundMap = await targetTransport.HasObjects(childrenIds).ConfigureAwait(false);
    List<string> newChildrenIds = new(from objId in childrenFoundMap.Keys where !childrenFoundMap[objId] select objId);

    targetTransport.BeginWrite();

    await api.DownloadObjects(
        StreamId,
        newChildrenIds,
        (childId, childData) =>
        {
          stopwatch.Stop();
          targetTransport.SaveObject(childId, childData);
          OnProgressAction?.Invoke(TransportName, 1);
          stopwatch.Start();
        }
      )
      .ConfigureAwait(false);

    // pausing until writing to the target transport
    stopwatch.Stop();
    targetTransport.SaveObject(id, rootObjectJson);

    await targetTransport.WriteComplete().ConfigureAwait(false);
    targetTransport.EndWrite();
    stopwatch.Start();

    //
    // Blobs download
    //
    var localBlobTrimmedHashes = Directory
      .GetFiles(BlobStorageFolder)
      .Select(fileName => fileName.Split(Path.DirectorySeparatorChar).Last())
      .Where(fileName => fileName.Length > 10)
      .Select(fileName => fileName.Substring(0, Blob.LocalHashPrefixLength))
      .ToList();

    var newBlobIds = blobIds
      .Where(blobId => !localBlobTrimmedHashes.Contains(blobId.Substring(0, Blob.LocalHashPrefixLength)))
      .ToList();

    await api.DownloadBlobs(
        StreamId,
        newBlobIds,
        () =>
        {
          OnProgressAction?.Invoke(TransportName, 1);
        }
      )
      .ConfigureAwait(false);

    stopwatch.Stop();
    Elapsed += stopwatch.Elapsed;
    return rootObjectJson;
  }

  public string GetObject(string id)
  {
    CancellationToken.ThrowIfCancellationRequested();
    var stopwatch = Stopwatch.StartNew();
    var result = Api.DownloadSingleObject(StreamId, id).Result;
    stopwatch.Stop();
    Elapsed += stopwatch.Elapsed;
    return result;
  }

  public async Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
  {
    if (string.IsNullOrEmpty(StreamId))
      throw new InvalidOperationException($"Invalid StreamID {StreamId}");
    return await Api.HasObjects(StreamId, objectIds).ConfigureAwait(false);
  }

  public void SaveObject(string id, string serializedObject)
  {
    if (string.IsNullOrEmpty(StreamId))
      throw new InvalidOperationException($"Invalid StreamID {StreamId}");
    lock (_sendBufferLock)
    {
      if (IsInErrorState)
        return;
      _sendBuffer.Add((id, serializedObject));
      _isWriteComplete = false;
    }
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    if (string.IsNullOrEmpty(StreamId))
      throw new InvalidOperationException($"Invalid StreamID {StreamId}");

    var objectData = sourceTransport.GetObject(id);

    if (objectData is null)
      throw new TransportException(
        this,
        $"Cannot copy {id} from {sourceTransport.TransportName} to {TransportName} as source returned null"
      );

    SaveObject(id, objectData);
  }

  public void BeginWrite()
  {
    if (_shouldSendThreadRun || _sendingThread != null)
      throw new InvalidOperationException("ServerTransport already sending");
    TotalSentBytes = 0;
    SavedObjectCount = 0;

    _exception = null;
    _shouldSendThreadRun = true;
    _sendingThread = new Thread(SendingThreadMain) { Name = "ServerTransportSender", IsBackground = true };
    _sendingThread.Start();
  }

  public async Task WriteComplete()
  {
    while (true)
    {
      lock (_sendBufferLock)
        if (_isWriteComplete || IsInErrorState)
        {
          CancellationToken.ThrowIfCancellationRequested();

          if (_exception is not null)
            throw new TransportException(this, $"{TransportName} transport failed", _exception);

          return;
        }

      await Task.Delay(50).ConfigureAwait(false);
    }
  }

  public void EndWrite()
  {
    if (!_shouldSendThreadRun || _sendingThread == null)
      throw new InvalidOperationException("ServerTransport not sending");
    _shouldSendThreadRun = false;
    _sendingThread.Join();
    _sendingThread = null;
  }

  private void Initialize(string baseUri, string streamId, string authorizationToken, int timeoutSeconds = 60)
  {
    SpeckleLog.Logger.Information("Initializing a new Remote Transport for {baseUri}", baseUri);

    BaseUri = baseUri;
    StreamId = streamId;
    AuthorizationToken = authorizationToken;
    TimeoutSeconds = timeoutSeconds;

    Api = new ParallelServerApi(BaseUri, AuthorizationToken, BlobStorageFolder, TimeoutSeconds);
    Api.OnBatchSent = (num, size) =>
    {
      OnProgressAction?.Invoke(TransportName, num);
      TotalSentBytes += size;
      SavedObjectCount += num;
    };
  }

  public override string ToString()
  {
    return $"Server Transport @{Account.serverInfo.url}";
  }

  private static List<string> ParseChildrenIds(string json)
  {
    List<string> childrenIds = new();
    try
    {
      JObject doc1 = JObject.Parse(json);
      foreach (JToken prop in doc1["__closure"])
        childrenIds.Add(((JProperty)prop).Name);
    }
    catch (Exception)
    {
      // empty children list if no __closure key is found
    }
    return childrenIds;
  }

  [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
  private async void SendingThreadMain()
  {
    while (true)
    {
      var stopwatch = Stopwatch.StartNew();
      if (!_shouldSendThreadRun || CancellationToken.IsCancellationRequested)
        return;
      List<(string id, string data)>? buffer = null;
      lock (_sendBufferLock)
        if (_sendBuffer.Count > 0)
        {
          buffer = _sendBuffer;
          _sendBuffer = new();
        }
        else
        {
          _isWriteComplete = true;
        }

      if (buffer is null)
      {
        Thread.Sleep(100);
        continue;
      }
      try
      {
        var bufferObjects = buffer.Where(tuple => !tuple.Item1.Contains("blob")).ToList();
        var bufferBlobs = buffer.Where(tuple => tuple.Item1.Contains("blob")).ToList();

        List<string> objectIds = new(bufferObjects.Count);

        foreach ((string id, _) in bufferObjects)
          if (id != "blob")
            objectIds.Add(id);

        Dictionary<string, bool> hasObjects = await Api.HasObjects(StreamId, objectIds).ConfigureAwait(false);
        List<(string, string)> newObjects = new();
        foreach ((string id, object json) in bufferObjects)
          if (!hasObjects[id])
            newObjects.Add((id, (string)json));

        // Report the objects that are already on the server
        OnProgressAction?.Invoke(TransportName, hasObjects.Count - newObjects.Count);

        await Api.UploadObjects(StreamId, newObjects).ConfigureAwait(false);

        if (bufferBlobs.Count != 0)
        {
          var blobIdsToUpload = await Api.HasBlobs(StreamId, bufferBlobs).ConfigureAwait(false);
          var formattedIds = blobIdsToUpload.Select(id => $"blob:{id}").ToList();
          var newBlobs = bufferBlobs.Where(tuple => formattedIds.IndexOf(tuple.Item1) != -1).ToList();
          if (newBlobs.Count != 0)
            await Api.UploadBlobs(StreamId, newBlobs).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        lock (_sendBufferLock)
        {
          _sendBuffer.Clear();
          _exception = ex;
        }
        return;
      }
      finally
      {
        stopwatch.Stop();
        lock (_elapsedLock)
          Elapsed += stopwatch.Elapsed;
      }
    }
  }
}
