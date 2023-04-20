#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Serialisation;

namespace Speckle.Core.Transports.ServerUtils;

internal enum ServerApiOperation
{
  _NoOp = default,
  DownloadSingleObject,
  DownloadObjects,
  HasObjects,
  UploadObjects,
  UploadBlobs,
  DownloadBlobs,
  HasBlobs,
}

internal class ParallelServerApi : ParallelOperationExecutor<ServerApiOperation>, IServerApi
{
  private string AuthToken;

  private string BaseUri;

  private object CallbackLock = new();

  private int TimeoutSeconds;

  public ParallelServerApi(
    string baseUri,
    string authorizationToken,
    string blobStorageFolder,
    int timeoutSeconds,
    int numThreads = 4,
    int numBufferedOperations = 8
  )
  {
    BaseUri = baseUri;
    AuthToken = authorizationToken;
    TimeoutSeconds = timeoutSeconds;
    NumThreads = numThreads;
    CancellationToken = CancellationToken.None;

    BlobStorageFolder = blobStorageFolder;

    NumThreads = numThreads;
    Tasks = new BlockingCollection<OperationTask<ServerApiOperation>>(numBufferedOperations);
  }

  public CancellationToken CancellationToken { get; set; }
  public bool CompressPayloads { get; set; } = true;
  public Action<int, int> OnBatchSent { get; set; }

  public string BlobStorageFolder { get; set; }

  #region Operations

  public async Task<Dictionary<string, bool>> HasObjects(string streamId, List<string> objectIds)
  {
    EnsureStarted();
    List<Task<object?>> tasks = new();
    List<List<string>> splitObjectsIds;
    if (objectIds.Count <= 50)
      splitObjectsIds = new List<List<string>> { objectIds };
    else
      splitObjectsIds = SplitList(objectIds, NumThreads);

    for (int i = 0; i < NumThreads; i++)
    {
      if (splitObjectsIds.Count <= i || splitObjectsIds[i].Count == 0)
        continue;
      var op = QueueOperation(ServerApiOperation.HasObjects, (streamId, splitObjectsIds[i]));
      tasks.Add(op);
    }
    Dictionary<string, bool> ret = new();
    foreach (var task in tasks)
    {
      Dictionary<string, bool> taskResult = await task.ConfigureAwait(false) as Dictionary<string, bool>;
      foreach (KeyValuePair<string, bool> kv in taskResult)
        ret[kv.Key] = kv.Value;
    }

    return ret;
  }

  public async Task<string> DownloadSingleObject(string streamId, string objectId)
  {
    EnsureStarted();
    Task<object?> op = QueueOperation(ServerApiOperation.DownloadSingleObject, (streamId, objectId));
    object? result = await op.ConfigureAwait(false);
    return (string)result!;
  }

  public async Task DownloadObjects(string streamId, List<string> objectIds, CbObjectDownloaded onObjectCallback)
  {
    EnsureStarted();
    List<Task<object?>> tasks = new();
    List<List<string>> splitObjectsIds = SplitList(objectIds, NumThreads);
    object callbackLock = new();

    CbObjectDownloaded callbackWrapper = (id, json) =>
    {
      lock (callbackLock)
        onObjectCallback(id, json);
    };

    for (int i = 0; i < NumThreads; i++)
    {
      if (splitObjectsIds[i].Count == 0)
        continue;
      Task<object?> op = QueueOperation(
        ServerApiOperation.DownloadObjects,
        (streamId, splitObjectsIds[i], callbackWrapper)
      );
      tasks.Add(op);
    }
    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
  }

  public async Task UploadObjects(string streamId, List<(string, string)> objects)
  {
    EnsureStarted();
    List<Task<object?>> tasks = new();
    List<List<(string, string)>> splitObjects;

    // request count optimization: if objects are < 500k, send in 1 request
    int totalSize = 0;
    foreach ((_, string json) in objects)
    {
      totalSize += json.Length;
      if (totalSize >= 500_000)
        break;
    }
    splitObjects = totalSize >= 500_000 ? SplitList(objects, NumThreads) : new() { objects };

    for (int i = 0; i < NumThreads; i++)
    {
      if (splitObjects.Count <= i || splitObjects[i].Count == 0)
        continue;
      var op = QueueOperation(ServerApiOperation.UploadObjects, (streamId, splitObjects[i]));
      tasks.Add(op);
    }
    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
  }

  public async Task UploadBlobs(string streamId, List<(string, string)> blobs)
  {
    EnsureStarted();
    var op = QueueOperation(ServerApiOperation.UploadBlobs, (streamId, blobs));
    await op.ConfigureAwait(false);
  }

  public async Task DownloadBlobs(string streamId, List<string> blobIds, CbBlobdDownloaded onBlobDownloaded)
  {
    EnsureStarted();
    var op = QueueOperation(ServerApiOperation.DownloadBlobs, (streamId, blobIds, onBlobDownloaded));
    await op.ConfigureAwait(false);
  }

  public async Task<List<string>?> HasBlobs(string streamId, List<(string, string)> blobs)
  {
    EnsureStarted();
    Task<object?> op = QueueOperation(ServerApiOperation.HasBlobs, (streamId, blobs));
    var res = await op.ConfigureAwait(false);
    return (List<string>?)res;
  }

  #endregion

  public void EnsureStarted()
  {
    if (Threads.Count == 0)
      Start();
  }

  protected override void ThreadMain()
  {
    using ServerApi serialApi = new(BaseUri, AuthToken, BlobStorageFolder, TimeoutSeconds);

    serialApi.OnBatchSent = (num, size) =>
    {
      lock (CallbackLock)
        OnBatchSent(num, size);
    };
    serialApi.CancellationToken = CancellationToken;
    serialApi.CompressPayloads = CompressPayloads;

    while (true)
    {
      var (operation, inputValue, tcs) = Tasks.Take();
      if (operation == ServerApiOperation._NoOp || tcs == null)
        return;

      try
      {
        var result = RunOperation(operation, inputValue!, serialApi).GetAwaiter().GetResult();
        tcs.SetResult(result);
      }
      catch (Exception ex)
      {
        tcs.SetException(ex);
      }
    }
  }

  private static async Task<object?> RunOperation(ServerApiOperation operation, object inputValue, ServerApi serialApi)
  {
    switch (operation)
    {
      case ServerApiOperation.DownloadSingleObject:
        var (dsoStreamId, dsoObjectId) = ((string, string))inputValue;
        return await serialApi.DownloadSingleObject(dsoStreamId, dsoObjectId).ConfigureAwait(false);
      case ServerApiOperation.DownloadObjects:
        var (doStreamId, doObjectIds, doCallback) = ((string, List<string>, CbObjectDownloaded))inputValue;
        await serialApi.DownloadObjects(doStreamId, doObjectIds, doCallback).ConfigureAwait(false);
        return null;
      case ServerApiOperation.HasObjects:
        var (hoStreamId, hoObjectIds) = ((string, List<string>))inputValue;
        return await serialApi.HasObjects(hoStreamId, hoObjectIds).ConfigureAwait(false);
      case ServerApiOperation.UploadObjects:
        var (uoStreamId, uoObjects) = ((string, List<(string, string)>))inputValue;
        await serialApi.UploadObjects(uoStreamId, uoObjects).ConfigureAwait(false);
        return null;
      case ServerApiOperation.UploadBlobs:
        var (ubStreamId, ubBlobs) = ((string, List<(string, string)>))inputValue;
        await serialApi.UploadBlobs(ubStreamId, ubBlobs).ConfigureAwait(false);
        return null;
      case ServerApiOperation.HasBlobs:
        var (hbStreamId, hBlobs) = ((string, List<(string, string)>))inputValue;
        return await serialApi
          .HasBlobs(hbStreamId, hBlobs.Select(b => b.Item1.Split(':')[1]).ToList())
          .ConfigureAwait(false);
      case ServerApiOperation.DownloadBlobs:
        var (dbStreamId, blobIds, cb) = ((string, List<string>, CbBlobdDownloaded))inputValue;
        await serialApi.DownloadBlobs(dbStreamId, blobIds, cb).ConfigureAwait(false);
        return null;
      default:
        throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
    }
  }

  private Task<object?> QueueOperation(ServerApiOperation operation, object? inputValue)
  {
    TaskCompletionSource<object?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Tasks.Add(new(operation, inputValue, tcs));
    return tcs.Task;
  }

  private static List<List<T>> SplitList<T>(List<T> list, int parts)
  {
    List<List<T>> ret = new(parts);
    for (int i = 0; i < parts; i++)
      ret.Add(new List<T>(list.Count / parts + 1));
    for (int i = 0; i < list.Count; i++)
      ret[i % parts].Add(list[i]);
    return ret;
  }
}
