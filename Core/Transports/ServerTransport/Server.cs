using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.Core.Transports
{
  /// <summary>
  /// Sends data to a speckle server. 
  /// TODOs:
  /// - gzip
  /// - preflight deltas on sending data
  /// - preflight deltas on receving/copying data to an existing transport? 
  /// </summary>
  public class ServerTransport : IDisposable, ITransport
  {
    public string TransportName { get; set; } = "RemoteTransport";

    public CancellationToken CancellationToken { get; set; }

    public string BaseUri { get; private set; }

    public string StreamId { get; set; }

    private HttpClient Client { get; set; }

    private ConcurrentQueue<(string, string, int)> Queue = new ConcurrentQueue<(string, string, int)>();

    private System.Timers.Timer WriteTimer;

    private int TotalElapsed = 0, PollInterval = 100;

    private bool IS_WRITING = false;

    private int MAX_BUFFER_SIZE = 100_000;

    private int MAX_MULTIPART_COUNT = 500;

    public bool CompressPayloads { get; set; } = true;

    public int SavedObjectCount { get; private set; } = 0;

    public int TotalSentBytes { get; set; } = 0;

    public Action<string, int> OnProgressAction { get; set; }

    public Action<string, Exception> OnErrorAction { get; set; }

    public Account Account { get; set; }

    public ServerTransport(Account account, string streamId, int timeoutSeconds = 60)
    {
      Account = account;
      Initialize(account.serverInfo.url, streamId, account.token, timeoutSeconds);
    }

    private void Initialize(string baseUri, string streamId, string authorizationToken, int timeoutSeconds = 60)
    {
      Log.AddBreadcrumb("New Remote Transport");

      BaseUri = baseUri;
      StreamId = streamId;

      Client = new HttpClient(new HttpClientHandler()
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip,
      })
      {
        BaseAddress = new Uri(baseUri),
        Timeout = new TimeSpan(0, 0, timeoutSeconds),
      };

      if (authorizationToken.ToLowerInvariant().Contains("bearer"))
      {
        Client.DefaultRequestHeaders.Add("Authorization", authorizationToken);
      }
      else
      {
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authorizationToken}");
      }
      WriteTimer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = PollInterval };
      WriteTimer.Elapsed += WriteTimerElapsed;
    }

    public void BeginWrite()
    {
      if (!GetWriteCompletionStatus())
      {
        throw new Exception("Transport is still writing.");
      }
      TotalSentBytes = 0;
      SavedObjectCount = 0;
    }

    public void EndWrite() { }

    #region Writing objects

    public async Task WriteComplete()
    {
      await Utilities.WaitUntil(() => { return GetWriteCompletionStatus(); }, 50);
    }

    public bool GetWriteCompletionStatus()
    {
      return Queue.Count == 0 && !IS_WRITING;
    }

    private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
    {
      TotalElapsed += PollInterval;

      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        IS_WRITING = false;
        return;
      }

      if (TotalElapsed > 300 && IS_WRITING == false && Queue.Count != 0)
      {
        TotalElapsed = 0;
        WriteTimer.Enabled = false;
#pragma warning disable CS4014 
        ConsumeQueue();
#pragma warning restore CS4014
      }
    }

    private async Task ConsumeQueue()
    {
      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        IS_WRITING = false;
        return;
      }

      if (Queue.Count == 0)
      {
        return;
      }

      IS_WRITING = true;
      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}", UriKind.Relative),
        Method = HttpMethod.Post
      };

      var multipart = new MultipartFormDataContent("--obj--");

      ValueTuple<string, string, int> result;
      SavedObjectCount = 0;
      var addedMpCount = 0;

      while (addedMpCount < MAX_MULTIPART_COUNT && Queue.Count != 0)
      {
        if (CancellationToken.IsCancellationRequested)
        {
          Queue = new ConcurrentQueue<(string, string, int)>();
          IS_WRITING = false;
          return;
        }

        var _ct = "[";
        var payloadBufferSize = 0;
        var i = 0;
        while (Queue.TryPeek(out result) && payloadBufferSize < MAX_BUFFER_SIZE)
        {
          if (CancellationToken.IsCancellationRequested)
          {
            Queue = new ConcurrentQueue<(string, string, int)>();
            return;
          }

          Queue.TryDequeue(out result);
          if (i != 0)
          {
            _ct += ",";
          }

          _ct += result.Item2;
          payloadBufferSize += result.Item3;
          TotalSentBytes += result.Item3;
          i++;
        }
        _ct += "]";

        if (CompressPayloads)
        {
          var content = new GzipContent(new StringContent(_ct, Encoding.UTF8));
          content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/gzip");
          multipart.Add(content, $"batch-{i}", $"batch-{i}");
        }
        else
        {
          multipart.Add(new StringContent(_ct, Encoding.UTF8), $"batch-{i}", $"batch-{i}");
        }

        addedMpCount++;
        SavedObjectCount += i;
      }

      message.Content = multipart;

      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        IS_WRITING = false;
        return;
      }

      try
      {
        var response = await Client.SendAsync(message, CancellationToken);
        response.EnsureSuccessStatusCode();
      }
      catch (Exception e)
      {
        IS_WRITING = false;
        OnErrorAction?.Invoke(TransportName, new Exception($"Remote error: {Account.serverInfo.url} is not reachable. \n {e.Message}", e));

        Queue = new ConcurrentQueue<(string, string, int)>();
        return;
      }

      //message.Headers.

      IS_WRITING = false;

      OnProgressAction?.Invoke(TransportName, SavedObjectCount);

      if (!WriteTimer.Enabled)
      {
        WriteTimer.Enabled = true;
        WriteTimer.Start();
      }
    }

    public void SaveObject(string hash, string serializedObject)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        IS_WRITING = false;
        return;
      }

      Queue.Enqueue((hash, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

      if (!WriteTimer.Enabled && !IS_WRITING)
      {
        WriteTimer.Enabled = true;
        WriteTimer.Start();
      }
    }

    public void SaveObject(string hash, ITransport sourceTransport)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        IS_WRITING = false;
        return;
      }

      var serializedObject = sourceTransport.GetObject(hash);

      Queue.Enqueue((hash, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

      if (!WriteTimer.Enabled && !IS_WRITING)
      {
        WriteTimer.Enabled = true;
        WriteTimer.Start();
      }
    }

    #endregion

    #region Getting objects

    public string GetObject(string hash)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        return null;
      }

      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}/{hash}/single", UriKind.Relative),
        Method = HttpMethod.Get,
      };

      var response = Client.SendAsync(message, HttpCompletionOption.ResponseContentRead, CancellationToken).Result.Content;
      return response.ReadAsStringAsync().Result;
    }

    public async Task<string> CopyObjectAndChildren(string hash, ITransport targetTransport, Action<int> onTotalChildrenCountKnown)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        return null;
      }

      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}/{hash}", UriKind.Relative),
        Method = HttpMethod.Get,
      };

      message.Headers.Add("Accept", "text/plain");
      string commitObj = null;

      HttpResponseMessage response = null;
      try
      {
        response = await Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, CancellationToken);
        response.EnsureSuccessStatusCode();
      }
      catch (Exception e)
      {
        OnErrorAction?.Invoke(TransportName, e);
      }


      var i = 0;
      using (var stream = await response.Content.ReadAsStreamAsync())
      {
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
          while (reader.Peek() > 0)
          {
            if (CancellationToken.IsCancellationRequested)
            {
              Queue = new ConcurrentQueue<(string, string, int)>();
              return null;
            }

            var line = reader.ReadLine();
            var pcs = line.Split(new char[] { '\t' }, count: 2);
            targetTransport.SaveObject(pcs[0], pcs[1]);
            if (i == 0)
            {
              commitObj = pcs[1];
              var partial = JsonConvert.DeserializeObject<Placeholder>(commitObj);
              if (partial.__closure != null)
                onTotalChildrenCountKnown?.Invoke(partial.__closure.Count);
            }
            OnProgressAction?.Invoke(TransportName, 1); // possibly make this more friendly
            i++;
          }
        }
      }

      await targetTransport.WriteComplete();
      return commitObj;
    }

    #endregion

    public override string ToString()
    {
      return $"Server Transport @{Account.serverInfo.url}";
    }

    public void Dispose()
    {
      // TODO: check if it's writing first? 
      Client.Dispose();
    }

    internal class Placeholder
    {
      public Dictionary<string, int> __closure { get; set; } = new Dictionary<string, int>();
    }
  }

  /// <summary>
  /// https://cymbeline.ch/2014/03/16/gzip-encoding-an-http-post-request-body/
  /// </summary>
  internal sealed class GzipContent : HttpContent
  {
    private readonly HttpContent content;

    public GzipContent(HttpContent content)
    {
      if (content == null)
      {
        return;
      }

      this.content = content;

      // Keep the original content's headers ...
      if (content != null)
        foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
        {
          Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

      // ... and let the server know we've Gzip-compressed the body of this request.
      Headers.ContentEncoding.Add("gzip");
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
      // Open a GZipStream that writes to the specified output stream.
      using (GZipStream gzip = new GZipStream(stream, CompressionMode.Compress, true))
      {
        // Copy all the input content to the GZip stream.
        if (content != null)
          await content.CopyToAsync(gzip);
        else
          await (new System.Net.Http.StringContent(string.Empty)).CopyToAsync(gzip);
      }
    }

    protected override bool TryComputeLength(out long length)
    {
      length = -1;
      return false;
    }
  }
}
