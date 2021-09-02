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
    UploadObjects,
    _NoOp
  }

  public class ParallelServerApi : IDisposable, IServerApi
  {
    private List<Thread> Threads = new List<Thread>();

    private string BaseUri;
    private string AuthToken;
    private int TimeoutSeconds;
    public CancellationToken CancellationToken { get; set; }
    public int NumThreads { get; set; }
    public Action<int, int> OnBatchSent { get; set; }

    private object LockFreeThreads = new object();
    private int FreeThreadCount = 0;
    private BlockingCollection<(ServerApiOperation, object, TaskCompletionSource<object>)> Tasks;

    public ParallelServerApi(string baseUri, string authorizationToken, int timeoutSeconds = 60, int numThreads = 2, int numBufferedOperations = 8)
    {
      BaseUri = baseUri;
      AuthToken = authorizationToken;
      TimeoutSeconds = timeoutSeconds;
      NumThreads = numThreads;

      Tasks = new BlockingCollection<(ServerApiOperation, object, TaskCompletionSource<object>)>(numBufferedOperations);
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
        Tasks.Add((ServerApiOperation._NoOp, null, null));
      }
      foreach (Thread t in Threads)
      {
        t.Join();
      }
      Threads = new List<Thread>();
    }

    private void ThreadMain()
    {
      using (ServerApi api = new ServerApi(BaseUri, AuthToken, TimeoutSeconds, CancellationToken))
      {
        while (true)
        {
          (ServerApiOperation operation, object inputValue, TaskCompletionSource<object> tcs) = Tasks.Take();
          if (tcs == null)
          {
            return;
          }

          try
          {
            switch(operation)
            {
              case ServerApiOperation.DownloadSingleObject:
                (string dsoStreamId, string dsoObjectId) = ((string, string))inputValue;
                var dsoResult = api.DownloadSingleObject(dsoStreamId, dsoObjectId).Result;
                tcs.SetResult(dsoResult);
                break;
              case ServerApiOperation.DownloadObjects:
                (string doStreamId, List<string> doObjectIds, CbObjectDownloaded doCallback) = ((string, List<string>, CbObjectDownloaded))inputValue;
                api.DownloadObjects(doStreamId, doObjectIds, doCallback).Wait();
                // TODO: pass errors?
                tcs.SetResult(null);
                break;
              case ServerApiOperation.HasObjects:
                (string hoStreamId, List<string> hoObjectIds) = ((string, List<string>))inputValue;
                var hoResult = api.HasObjects(hoStreamId, hoObjectIds).Result;
                tcs.SetResult(hoResult);
                break;
              case ServerApiOperation.UploadObjects:
                (string uoStreamId, List<(string, string)> uoObjects) = ((string, List<(string, string)>))inputValue;
                api.UploadObjects(uoStreamId, uoObjects).Wait();
                // TODO: pass errors?
                tcs.SetResult(null);
                break;
            }
          }
          catch (Exception e)
          {
            tcs.SetException(e);
          }

        }
      }
    }

    private Task<object> QueueOperation(ServerApiOperation operation, object inputValue)
    {
      TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
      Tasks.Add((operation, inputValue, tcs));
      return tcs.Task;
    }

    public async Task<Dictionary<string, bool>> HasObjects(string streamId, List<string> objectIds)
    {
      Task<object> op = QueueOperation(ServerApiOperation.HasObjects, (streamId, objectIds));
      object result = await op;
      return result as Dictionary<string, bool>;
    }

    public async Task<string> DownloadSingleObject(string streamId, string objectId)
    {
      Task<object> op = QueueOperation(ServerApiOperation.DownloadSingleObject, (streamId, objectId));
      object result = await op;
      return result as string;
    }

    public async Task DownloadObjects(string streamId, List<string> objectIds, CbObjectDownloaded onObjectCallback)
    {
      Task<object> op = QueueOperation(ServerApiOperation.DownloadObjects, (streamId, objectIds, onObjectCallback));
      object result = await op;
    }

    public async Task UploadObjects(string streamId, List<(string, string)> objects)
    {
      Task<object> op = QueueOperation(ServerApiOperation.UploadObjects, (streamId, objects));
      object result = await op;
    }

    public void Dispose()
    {
      if (Threads.Count > 0)
        Stop();
      Tasks.Dispose();
    }
  }
}
