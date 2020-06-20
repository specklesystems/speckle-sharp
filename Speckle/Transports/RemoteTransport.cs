using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Speckle.Http;

namespace Speckle.Transports
{
  public class RemoteTransport : IDisposable, ITransport
  {
    public string TransportName { get; set; } = "RemoteTransport";

    public string BaseUri { get; private set; }

    public string StreamId { get; private set; }

    private HttpClient Client { get; set; }

    private List<(string, string)> Buffer = new List<(string, string)>();

    private System.Timers.Timer WriteTimer;

    private int TotalElapsed = 0, PollInterval = 50;

    private bool IS_WRITING = false;

    private int MAX_BUFFER_SIZE = 200000; // 100k
    private int MAX_MULTIPART_COUNT = 5;
    private int CURR_BUFFER_SIZE = 0;

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

      WriteTimer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = PollInterval };
      WriteTimer.Elapsed += WriteTimerElapsed;

    }

    public async Task WriteComplete()
    {
      await Utilities.WaitUntil(() => { return GetWriteCompletionStatus(); }, 100);
    }

    public bool GetWriteCompletionStatus()
    {
      return Buffer.Count == 0 && !IS_WRITING;
    }

    private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
    {
      TotalElapsed += PollInterval;
      if (TotalElapsed > 300 && IS_WRITING == false)
      {
        TotalElapsed = 0;
        WriteTimer.Enabled = false;
        if (Buffer.Count > 0)
          WriteBuffer2();
      }
    }

    private async Task WriteBuffer()
    {
      IS_WRITING = true;
      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri("/objects/testStreamId", UriKind.Relative),
        Method = HttpMethod.Post
      };

      var _content = "[";
      var payloadBufferSize = 0;
      var i = 0;

      lock (Buffer)
      {
        if (Buffer.Count == 0)
          return;

        while (payloadBufferSize < MAX_BUFFER_SIZE && i < Buffer.Count)
        {
          var (hash, obj) = Buffer[i++];
          var len = System.Text.Encoding.UTF8.GetByteCount(obj);
          payloadBufferSize += len;
          CURR_BUFFER_SIZE -= len;
          if (i != 1) _content += ",";
          _content += obj;
        }
        Buffer.RemoveRange(0, i);
        _content += "]";
      }
      var stringContent = new StringContent(_content);
      stringContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
      message.Content = new GzipContent(stringContent);

      Console.WriteLine($"Sending {payloadBufferSize} bytes ({i} objs)to remote");

      var res = await Client.SendAsync(message);
      var ids = await res.Content.ReadAsStringAsync();
      //Console.WriteLine($"{ids.Split(",").Count()} written to remote {Client.BaseAddress}");

      IS_WRITING = false;

      if (CURR_BUFFER_SIZE > MAX_BUFFER_SIZE)
      {
        WriteBuffer();
      }
      else if (Buffer.Count > 0)
      {
        WriteTimer.Enabled = true;
        WriteTimer.Start();
      }
    }

    private async Task WriteBuffer2()
    {
      IS_WRITING = true;
      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri("/objects/multipart/testStreamId", UriKind.Relative),
        Method = HttpMethod.Post
      };

      var multipart = new MultipartFormDataContent("----obj");

      var contents = new List<string>();

      lock (Buffer)
      {
        if (Buffer.Count == 0)
          return;

        while (contents.Count < MAX_MULTIPART_COUNT && Buffer.Count != 0)
        {
          var _content = "[";
          var payloadBufferSize = 0;
          var i = 0;

          while (payloadBufferSize < MAX_BUFFER_SIZE && i < Buffer.Count)
          {
            var (hash, obj) = Buffer[i++];
            var len = System.Text.Encoding.UTF8.GetByteCount(obj);
            payloadBufferSize += len;
            CURR_BUFFER_SIZE -= len;
            if (i != 1) _content += ",";
            _content += obj;
          }
          Buffer.RemoveRange(0, i);
          _content += "]";

          contents.Add(_content);
        }
      }

      foreach(var _ct in contents)
      {
        multipart.Add(new StringContent(_ct, Encoding.UTF8), "a", "a");
      }

      //multipart.Add(new StringContent(_content, Encoding.UTF8), "b", "b");
      //multipart.Add(new StringContent(_content, Encoding.UTF8), "c", "c");
      ////multipart.Add(new GzipContent(new StringContent(_content)), "a", "a");

      //var stringContent = new StringContent(_content);
      //stringContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
      //message.Content = new GzipContent(multipart);
      message.Content = multipart;

      //Console.WriteLine($"Sending {payloadBufferSize} bytes ({i} objs)to remote");

      var res = await Client.SendAsync(message);
      var ids = await res.Content.ReadAsStringAsync();
      //Console.WriteLine($"{ids.Split(",").Count()} written to remote {Client.BaseAddress}");

      IS_WRITING = false;

      if (CURR_BUFFER_SIZE > MAX_BUFFER_SIZE)
      {
        WriteBuffer2();
      }
      else if (Buffer.Count > 0)
      {
        WriteTimer.Enabled = true;
        WriteTimer.Start();
      }
    }

    public void SaveObject(string hash)
    {

    }

    public void SaveObject(string hash, string serializedObject)
    {
      CURR_BUFFER_SIZE += System.Text.Encoding.UTF8.GetByteCount(serializedObject);
      lock (Buffer)
      {
        Buffer.Add((hash, serializedObject));
        if (!WriteTimer.Enabled)
        {
          WriteTimer.Enabled = true;
          WriteTimer.Start();
        }
      }
    }

    public string GetObject(string hash)
    {
      throw new NotImplementedException();
    }


    public void Dispose()
    {
      throw new NotImplementedException();
    }
  }
}
