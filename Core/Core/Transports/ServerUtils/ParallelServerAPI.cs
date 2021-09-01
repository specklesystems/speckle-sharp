using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Speckle.Core.Transports.ServerUtils
{
  internal enum ServerApiOperation
  {
    DownloadSingleObject,
    DownloadObjects,
    HasObjects,
    UploadObjects
  }

  internal class ParallelServerAPI : IDisposable
  {
    private List<Thread> Threads = new List<Thread>();

    private string BaseUri;
    private string AuthToken;
    private int TimeoutSeconds;
    public CancellationToken CancellationToken { get; set; }
    public int NumThreads { get; set; }

    private object LockFreeThreads = new object();
    private int FreeThreadCount = 0;
    private BlockingCollection<(ServerApiOperation, object, TaskCompletionSource<object>)> Tasks = new BlockingCollection<(ServerApiOperation, object, TaskCompletionSource<object>)>();

    public ParallelServerAPI(string baseUri, string authorizationToken, int timeoutSeconds = 60, CancellationToken cancellationToken = default(CancellationToken), int numThreads = 4)
    {
      BaseUri = baseUri;
      AuthToken = authorizationToken;
      TimeoutSeconds = timeoutSeconds;
      CancellationToken = cancellationToken;
      NumThreads = numThreads;
    }

    public void Start()
    {
      if (Threads.Count > 0)
        throw new Exception("ServerAPI: Threads already started");
      for (int i = 0; i < NumThreads; i++)
      {
        Thread t = new Thread(new ThreadStart(ThreadMain));
        t.IsBackground = true;
        Threads.Add(t);
        t.Start();
      }
    }

    public void Stop()
    {
      if (Threads.Count == 0)
        throw new Exception("ServerAPI: Threads not started");
      foreach (Thread t in Threads)
      {
        // TODO: signal to stop
      }
      foreach (Thread t in Threads)
      {
        t.Join();
      }
    }

    private void ThreadMain()
    {
      using (ServerAPI api = new ServerAPI(BaseUri, AuthToken, TimeoutSeconds, CancellationToken))
      {
        while (true)
        {
          lock (LockFreeThreads)
          {
            FreeThreadCount++;
          }
          (ServerApiOperation operation, object inputValue, TaskCompletionSource<object> tcs) = Tasks.Take();
          if (tcs == null)
          {
            return;
          }

          try
          {
            switch(operation)
            {
              case ServerApiOperation.HasObjects:
                (string streamId, List<string> objectIds) = ((string, List<string>))inputValue;
                tcs.SetResult(api.HasObjects(streamId, objectIds));
                continue;
              case ServerApiOperation.DownloadObjects:
                continue;
              case ServerApiOperation.UploadObjects:
                continue;
            }
          }
          catch (Exception e)
          {
            tcs.SetException(e);
          }

        }
      }
    }

    public async Task<Dictionary<string, bool>> HasObjects(string streamId, List<string> objectIds)
    {
      return await API.HasObjects(streamId, objectIds);
    }

    public void Dispose()
    {
      if (Threads.Count > 0)
        Stop();
    }
  }
}
