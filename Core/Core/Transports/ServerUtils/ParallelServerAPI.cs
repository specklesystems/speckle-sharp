#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Serialisation;

namespace Speckle.Core.Transports.ServerUtils;

internal enum ServerApiOperation
{
  NoOp = default,
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
  private readonly string _authToken;

  private readonly string _baseUri;

  private readonly object _callbackLock = new();

  private readonly int _timeoutSeconds;

  public ParallelServerApi(
    string baseUri,
    string authorizationToken,
    string blobStorageFolder,
    int timeoutSeconds,
    int numThreads = 4,
    int numBufferedOperations = 8
  )
  {
    _baseUri = baseUri;
    _authToken = authorizationToken;
    _timeoutSeconds = timeoutSeconds;
    NumThreads = numThreads;

    BlobStorageFolder = blobStorageFolder;

    NumThreads = numThreads;
    Tasks = new BlockingCollection<OperationTask<ServerApiOperation>>(numBufferedOperations);
  }

  public CancellationToken CancellationToken { get; set; }
  public bool CompressPayloads { get; set; } = true;
  public Action<int, int> OnBatchSent { get; set; }

  public string BlobStorageFolder { get; set; }

  #region Operations

  public async Task<Dictionary<string, bool>> HasObjects(string streamId, IReadOnlyList<string> objectIds)
  {
    EnsureStarted();
    List<Task<object?>> tasks = new();
    IReadOnlyList<IReadOnlyList<string>> splitObjectsIds;
    if (objectIds.Count <= 50)
      splitObjectsIds = new List<IReadOnlyList<string>> { objectIds };
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

  public async Task DownloadObjects(
    string streamId,
    IReadOnlyList<string> objectIds,
    CbObjectDownloaded onObjectCallback
  )
  {
    EnsureStarted();
    List<Task<object?>> tasks = new();
    IReadOnlyList<IReadOnlyList<string>> splitObjectsIds = SplitList(objectIds, NumThreads);
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

  public async Task UploadObjects(string streamId, IReadOnlyList<(string, string)> objects)
  {
    EnsureStarted();
    List<Task<object?>> tasks = new();
    IReadOnlyList<IReadOnlyList<(string, string)>> splitObjects;

    // request count optimization: if objects are < 500k, send in 1 request
    int totalSize = 0;
    foreach ((_, string json) in objects)
    {
      totalSize += json.Length;
      if (totalSize >= 500_000)
        break;
    }
    splitObjects =
      totalSize >= 500_000 ? SplitList(objects, NumThreads) : new List<IReadOnlyList<(string, string)>> { objects };

    for (int i = 0; i < NumThreads; i++)
    {
      if (splitObjects.Count <= i || splitObjects[i].Count == 0)
        continue;
      var op = QueueOperation(ServerApiOperation.UploadObjects, (streamId, splitObjects[i]));
      tasks.Add(op);
    }
    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
  }

  public async Task UploadBlobs(string streamId, IReadOnlyList<(string, string)> blobs)
  {
    EnsureStarted();
    var op = QueueOperation(ServerApiOperation.UploadBlobs, (streamId, blobs));
    await op.ConfigureAwait(false);
  }

  public async Task DownloadBlobs(string streamId, IReadOnlyList<string> blobIds, CbBlobdDownloaded onBlobDownloaded)
  {
    EnsureStarted();
    var op = QueueOperation(ServerApiOperation.DownloadBlobs, (streamId, blobIds, onBlobDownloaded));
    await op.ConfigureAwait(false);
  }

  public async Task<List<string>> HasBlobs(string streamId, IReadOnlyList<(string, string)> blobs)
  {
    EnsureStarted();
    Task<object?> op = QueueOperation(ServerApiOperation.HasBlobs, (streamId, blobs));
    var res = (List<string>)await op.ConfigureAwait(false);
    Debug.Assert(res is not null);
    return res!;
  }

  #endregion

  public void EnsureStarted()
  {
    if (Threads.Count == 0)
      Start();
  }

  [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
  protected override void ThreadMain()
  {
    using ServerApi serialApi = new(_baseUri, _authToken, BlobStorageFolder, _timeoutSeconds);

    serialApi.OnBatchSent = (num, size) =>
    {
      lock (_callbackLock)
        OnBatchSent(num, size);
    };
    serialApi.CancellationToken = CancellationToken;
    serialApi.CompressPayloads = CompressPayloads;

    while (true)
    {
      var (operation, inputValue, tcs) = Tasks.Take();
      if (operation == ServerApiOperation.NoOp || tcs == null)
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
        var (doStreamId, doObjectIds, doCallback) = ((string, IReadOnlyList<string>, CbObjectDownloaded))inputValue;
        await serialApi.DownloadObjects(doStreamId, doObjectIds, doCallback).ConfigureAwait(false);
        return null;
      case ServerApiOperation.HasObjects:
        var (hoStreamId, hoObjectIds) = ((string, IReadOnlyList<string>))inputValue;
        return await serialApi.HasObjects(hoStreamId, hoObjectIds).ConfigureAwait(false);
      case ServerApiOperation.UploadObjects:
        var (uoStreamId, uoObjects) = ((string, IReadOnlyList<(string, string)>))inputValue;
        await serialApi.UploadObjects(uoStreamId, uoObjects).ConfigureAwait(false);
        return null;
      case ServerApiOperation.UploadBlobs:
        var (ubStreamId, ubBlobs) = ((string, IReadOnlyList<(string, string)>))inputValue;
        await serialApi.UploadBlobs(ubStreamId, ubBlobs).ConfigureAwait(false);
        return null;
      case ServerApiOperation.HasBlobs:
        var (hbStreamId, hBlobs) = ((string, IReadOnlyList<(string, string)>))inputValue;
        return await serialApi
          .HasBlobs(hbStreamId, hBlobs.Select(b => b.Item1.Split(':')[1]).ToList())
          .ConfigureAwait(false);
      case ServerApiOperation.DownloadBlobs:
        var (dbStreamId, blobIds, cb) = ((string, IReadOnlyList<string>, CbBlobdDownloaded))inputValue;
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

  private static List<List<T>> SplitList<T>(IReadOnlyList<T> list, int parts)
  {
    List<List<T>> ret = new(parts);
    for (int i = 0; i < parts; i++)
      ret.Add(new List<T>(list.Count / parts + 1));
    for (int i = 0; i < list.Count; i++)
      ret[i % parts].Add(list[i]);
    return ret;
  }
}
