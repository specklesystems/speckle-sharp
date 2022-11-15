using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    UploadBlobs,
    DownloadBlobs,
    HasBlobs,
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
    public bool CompressPayloads { get; set; } = true;

    private object CallbackLock = new object();
    public Action<int, int> OnBatchSent { get; set; }

    private BlockingCollection<(ServerApiOperation, object, TaskCompletionSource<object>)> Tasks;

    public string BlobStorageFolder { get; set; }

    public ParallelServerApi(string baseUri, string authorizationToken, string blobStorageFolder, int timeoutSeconds = 60, int numThreads = 4, int numBufferedOperations = 8)
    {
      BaseUri = baseUri;
      AuthToken = authorizationToken;
      TimeoutSeconds = timeoutSeconds;
      NumThreads = numThreads;
      CancellationToken = CancellationToken.None;

      BlobStorageFolder = blobStorageFolder;

      Tasks = new BlockingCollection<(ServerApiOperation, object, TaskCompletionSource<object>)>(numBufferedOperations);
    }

    public void EnsureStarted()
    {
      if (Threads.Count == 0)
        Start();
    }

    public void Start()
    {
      if (Threads.Count > 0)
        throw new Exception("ServerAPI: Threads already started");
      for (int i = 0; i < NumThreads; i++)
      {
        Thread t = new Thread(new ThreadStart(ThreadMain));
        t.Name = $"ParallelServerAPI";
        t.IsBackground = true;
        Threads.Add(t);
        t.Start();
      }
    }

    public void EnsureStopped()
    {
      if (Threads.Count > 0)
        Stop();
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
      using (ServerApi serialApi = new ServerApi(BaseUri, AuthToken, BlobStorageFolder, TimeoutSeconds))
      {
        serialApi.OnBatchSent = (num, size) => { lock (CallbackLock) OnBatchSent(num, size); };
        serialApi.CancellationToken = CancellationToken;
        serialApi.CompressPayloads = CompressPayloads;

        while (true)
        {
          (ServerApiOperation operation, object inputValue, TaskCompletionSource<object> tcs) = Tasks.Take();
          if (tcs == null)
          {
            return;
          }

          try
          {
            switch (operation)
            {
              case ServerApiOperation.DownloadSingleObject:
                (string dsoStreamId, string dsoObjectId) = ((string, string))inputValue;
                var dsoResult = serialApi.DownloadSingleObject(dsoStreamId, dsoObjectId).Result;
                tcs.SetResult(dsoResult);
                break;
              case ServerApiOperation.DownloadObjects:
                (string doStreamId, List<string> doObjectIds, CbObjectDownloaded doCallback) = ((string, List<string>, CbObjectDownloaded))inputValue;
                serialApi.DownloadObjects(doStreamId, doObjectIds, doCallback).Wait();
                // TODO: pass errors?
                tcs.SetResult(null);
                break;
              case ServerApiOperation.HasObjects:
                (string hoStreamId, List<string> hoObjectIds) = ((string, List<string>))inputValue;
                var hoResult = serialApi.HasObjects(hoStreamId, hoObjectIds).Result;
                tcs.SetResult(hoResult);
                break;
              case ServerApiOperation.UploadObjects:
                (string uoStreamId, List<(string, string)> uoObjects) = ((string, List<(string, string)>))inputValue;
                serialApi.UploadObjects(uoStreamId, uoObjects).Wait();
                // TODO: pass errors?
                tcs.SetResult(null);
                break;
              case ServerApiOperation.UploadBlobs:
                (string ubStreamId, List<(string, string)> ubBlobs) = ((string, List<(string, string)>))inputValue;
                serialApi.UploadBlobs(ubStreamId, ubBlobs).Wait();
                tcs.SetResult(null);
                break;
              case ServerApiOperation.HasBlobs:
                (string hbStreamId, List<(string, string)> hBlobs) = ((string, List<(string, string)>))inputValue;
                var hasBlobResult = serialApi.HasBlobs(hbStreamId, hBlobs.Select(b => b.Item1.Split(':')[1]).ToList()).Result;
                tcs.SetResult(hasBlobResult);
                break;
              case ServerApiOperation.DownloadBlobs:
                (string dbStreamId, List<string> blobIds, CbBlobdDownloaded cb) = ((string, List<string>, CbBlobdDownloaded))inputValue;
                serialApi.DownloadBlobs(dbStreamId, blobIds, cb).Wait();
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

    private List<List<T>> SplitList<T>(List<T> list, int parts)
    {
      List<List<T>> ret = new List<List<T>>(parts);
      for (int i = 0; i < parts; i++)
        ret.Add(new List<T>(list.Count / parts + 1));
      for (int i = 0; i < list.Count; i++)
        ret[i % parts].Add(list[i]);
      return ret;
    }

    public async Task<Dictionary<string, bool>> HasObjects(string streamId, List<string> objectIds)
    {
      EnsureStarted();
      List<Task<object>> tasks = new List<Task<object>>();
      List<List<string>> splitObjectsIds;
      if (objectIds.Count <= 50)
        splitObjectsIds = new List<List<string>>() { objectIds };
      else
        splitObjectsIds = SplitList(objectIds, NumThreads);

      for (int i = 0; i < NumThreads; i++)
      {
        if (splitObjectsIds.Count <= i || splitObjectsIds[i].Count == 0)
          continue;
        Task<object> op = QueueOperation(ServerApiOperation.HasObjects, (streamId, splitObjectsIds[i]));
        tasks.Add(op);
      }
      Dictionary<string, bool> ret = new Dictionary<string, bool>();
      foreach (Task<object> task in tasks)
      {
        Dictionary<string, bool> taskResult = (await task) as Dictionary<string, bool>;
        foreach (KeyValuePair<string, bool> kv in taskResult)
          ret[kv.Key] = kv.Value;
      }

      return ret;
    }

    public async Task<string> DownloadSingleObject(string streamId, string objectId)
    {
      EnsureStarted();
      Task<object> op = QueueOperation(ServerApiOperation.DownloadSingleObject, (streamId, objectId));
      object result = await op;
      return result as string;
    }

    public async Task DownloadObjects(string streamId, List<string> objectIds, CbObjectDownloaded onObjectCallback)
    {
      // Stopwatch sw = new Stopwatch(); sw.Start(); // TODO: remove

      EnsureStarted();
      List<Task<object>> tasks = new List<Task<object>>();
      List<List<string>> splitObjectsIds = SplitList(objectIds, NumThreads);
      object callbackLock = new object();

      CbObjectDownloaded callbackWrapper = (string id, string json) =>
      {
        lock (callbackLock)
        {
          onObjectCallback(id, json);
        }
      };

      for (int i = 0; i < NumThreads; i++)
      {
        if (splitObjectsIds[i].Count == 0)
          continue;
        Task<object> op = QueueOperation(ServerApiOperation.DownloadObjects, (streamId, splitObjectsIds[i], callbackWrapper));
        tasks.Add(op);
      }
      await Task.WhenAll(tasks.ToArray());
      // Console.WriteLine($"ParallelServerApi::DownloadObjects({objectIds.Count}) request in {sw.ElapsedMilliseconds / 1000.0} sec");

    }

    public async Task UploadObjects(string streamId, List<(string, string)> objects)
    {
      // Stopwatch sw = new Stopwatch(); sw.Start();

      EnsureStarted();
      List<Task<object>> tasks = new List<Task<object>>();
      List<List<(string, string)>> splitObjects;

      // request count optimization: if objects are < 500k, send in 1 request
      int totalSize = 0;
      foreach ((string id, string json) in objects)
      {
        totalSize += json.Length;
        if (totalSize >= 500000)
          break;
      }
      if (totalSize < 500000)
        splitObjects = new List<List<(string, string)>>() { objects };
      else
        splitObjects = SplitList(objects, NumThreads);

      for (int i = 0; i < NumThreads; i++)
      {
        if (splitObjects.Count <= i || splitObjects[i].Count == 0)
          continue;
        Task<object> op = QueueOperation(ServerApiOperation.UploadObjects, (streamId, splitObjects[i]));
        tasks.Add(op);
      }
      await Task.WhenAll(tasks.ToArray());
      // Console.WriteLine($"ParallelServerApi::UploadObjects({objects.Count}) request in {sw.ElapsedMilliseconds / 1000.0} sec");
    }

    public async Task UploadBlobs(string streamId, List<(string, string)> blobs)
    {
      EnsureStarted();
      Task<object> op = QueueOperation(ServerApiOperation.UploadBlobs, (streamId, blobs));
      await op;
    }

    public async Task DownloadBlobs(string streamId, List<string> blobIds, CbBlobdDownloaded onBlobDownloaded)
    {
      EnsureStarted();
      Task<object> op = QueueOperation(ServerApiOperation.DownloadBlobs, (streamId, blobIds, onBlobDownloaded));
      await op;
    }

    public async Task<List<string>> HasBlobs(string streamId, List<(string, string)> blobs)
    {
      EnsureStarted();
      Task<object> op = QueueOperation(ServerApiOperation.HasBlobs, (streamId, blobs));
      var res = await op;
      return res as List<string>;
    }

    public void Dispose()
    {
      EnsureStopped();
      Tasks.Dispose();
    }
  }
}
