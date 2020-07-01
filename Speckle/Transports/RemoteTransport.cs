using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Speckle.Transports
{
  public class RemoteTransport : IDisposable, ITransport, IRemoteTransport
  {
    public string TransportName { get; set; } = "RemoteTransport";

    public string BaseUri { get; private set; }

    public string StreamId { get; private set; }

    public ITransport LocalTransport { get; set; }

    private HttpClient Client { get; set; }

    private ConcurrentQueue<(string, string, int)> Queue = new ConcurrentQueue<(string, string, int)>();

    private System.Timers.Timer WriteTimer;

    private int TotalElapsed = 0, PollInterval = 50;

    private bool IS_WRITING = false;

    private int MAX_BUFFER_SIZE = 250_000;
    private int MAX_MULTIPART_COUNT = 4;

    private int totalProcessedCount = 0;

    public Action<string, int> OnProgressAction;

    public RemoteTransport(string baseUri, string streamId, string authorizationToken, int timeoutSeconds = 60)
    {
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

      Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authorizationToken}");

      WriteTimer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = PollInterval };
      WriteTimer.Elapsed += WriteTimerElapsed;
    }

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
      if (TotalElapsed > 300 && IS_WRITING == false && Queue.Count != 0)
      {
        TotalElapsed = 0;
        WriteTimer.Enabled = false;
#pragma warning disable CS4014 
        ConsumeQueue();
#pragma warning restore CS4014
      }
    }

    // TODO: Gzip
    private async Task ConsumeQueue()
    {
      IS_WRITING = true;
      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}", UriKind.Relative),
        Method = HttpMethod.Post
      };

      var multipart = new MultipartFormDataContent("--obj--");
      var contents = new List<string>();

      ValueTuple<string, string, int> result;
      totalProcessedCount = 0;
      while (contents.Count < MAX_MULTIPART_COUNT && Queue.Count != 0)
      {
        var _ct = "[";
        var payloadBufferSize = 0;
        var i = 0;
        while (Queue.TryPeek(out result) && payloadBufferSize < MAX_BUFFER_SIZE)
        {
          Queue.TryDequeue(out result);
          if (i != 0) _ct += ",";
          _ct += result.Item2;
          payloadBufferSize += result.Item3;
          i++;
        }
        _ct += "]";
        multipart.Add(new StringContent(_ct, Encoding.UTF8), $"batch-{i}", $"batch-{i}");
        totalProcessedCount += i;
      }

      message.Content = multipart;
      try
      {
        await Client.SendAsync(message);
      }
      catch (Exception)
      {
        throw new Exception($"Remote unreachable ({Client.BaseAddress.ToString()})");
      }

      IS_WRITING = false;

      OnProgressAction?.Invoke($"POST Remote ({Client.BaseAddress.ToString()})", totalProcessedCount);

      if (!WriteTimer.Enabled)
      {
        WriteTimer.Enabled = true;
        WriteTimer.Start();
      }
    }

    public void SaveObject(string hash, string serializedObject)
    {
      if (serializedObject == null && LocalTransport == null)
        throw new Exception("Cannot push object by reference if no local transport is provided.");

      if (serializedObject == null)
        serializedObject = LocalTransport.GetObject(hash);

      Queue.Enqueue((hash, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

      if (!WriteTimer.Enabled && !IS_WRITING)
      {
        WriteTimer.Enabled = true;
        WriteTimer.Start();
      }
    }

    #endregion
    public string GetObject(string hash)
    {
      // TODO: Untested
      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}/{hash}/single", UriKind.Relative),
        Method = HttpMethod.Get,
      };

      var response = Client.SendAsync(message, HttpCompletionOption.ResponseContentRead).Result.Content;
      return response.ReadAsStringAsync().Result;
    }

    public async Task<string> GetObjectAndChildren(string hash)
    {

      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}/{hash}", UriKind.Relative),
        Method = HttpMethod.Get,
      };

      message.Headers.Add("Accept", "text/plain");
      string commitObj = null;
      

      var response = await Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
      var i = 0;
      using (var stream = await response.Content.ReadAsStreamAsync())
      {
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
          while (reader.Peek() > 0)
          {
            var line = reader.ReadLine();
            var pcs = line.Split(new char[] { '\t' }, count: 2);
            LocalTransport.SaveObject(pcs[0], pcs[1]);
            if (i == 0)
            {
              commitObj = pcs[1];
            }
            OnProgressAction?.Invoke($"GET Remote ({Client.BaseAddress.ToString()})", i++);
          }
        }
      }

      await ((SqlLiteObjectTransport)LocalTransport).WriteComplete();
      return commitObj;
    }


    public void Dispose()
    {
      // TODO: check if it's writing first? 
      Client.Dispose();
    }
  }
}
