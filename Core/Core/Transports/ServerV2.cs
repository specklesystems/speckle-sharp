using System;
using System.Collections.Generic;
using System.Diagnostics;
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
  public ServerTransport(Account account, string streamId, int timeoutSeconds = 60, string blobStorageFolder = null)
    : base(account, streamId, timeoutSeconds, blobStorageFolder) { }
}

public class ServerTransportV2 : IDisposable, ICloneable, ITransport, IBlobCapableTransport
{
  private object ElapsedLock = new();

  private bool ErrorState;
  private bool IsWriteComplete;

  // TODO: make send buffer more flexible to accept blobs too
  private List<(string, string)> SendBuffer = new();
  private object SendBufferLock = new();
  private Thread SendingThread;

  private bool ShouldSendThreadRun;

  public ServerTransportV2(Account account, string streamId, int timeoutSeconds = 60, string blobStorageFolder = null)
  {
    Account = account;
    CancellationToken = CancellationToken.None;
    Initialize(account.serverInfo.url, streamId, account.token, timeoutSeconds);

    if (blobStorageFolder == null)
      BlobStorageFolder = SpecklePathProvider.BlobStoragePath();
    Directory.CreateDirectory(BlobStorageFolder);
  }

  public int TotalSentBytes { get; set; }

  public Account Account { get; set; }
  public string BaseUri { get; private set; }
  public string StreamId { get; set; }

  public int TimeoutSeconds { get; set; }
  private string AuthorizationToken { get; set; }

  internal ParallelServerApi Api { get; private set; }

  public string BlobStorageFolder { get; set; }

  public void SaveBlob(Blob obj)
  {
    if (string.IsNullOrEmpty(StreamId) || obj == null)
      throw new InvalidOperationException("Invalid parameters to SaveBlob");
    var hash = obj.GetFileHash();

    lock (SendBufferLock)
    {
      if (ErrorState)
        return;
      SendBuffer.Add(($"blob:{hash}", obj.filePath));
    }
  }

  public object Clone()
  {
    return new ServerTransportV2(Account, StreamId)
    {
      OnErrorAction = OnErrorAction,
      OnProgressAction = OnProgressAction,
      CancellationToken = CancellationToken
    };
  }

  public void Dispose()
  {
    if (SendingThread != null)
    {
      ShouldSendThreadRun = false;
      SendingThread.Join();
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
  public Action<string, int> OnProgressAction { get; set; }
  public Action<string, Exception> OnErrorAction { get; set; }
  public int SavedObjectCount { get; private set; }
  public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

  public async Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int> onTotalChildrenCountKnown = null
  )
  {
    if (string.IsNullOrEmpty(StreamId) || string.IsNullOrEmpty(id) || targetTransport == null)
      throw new InvalidOperationException("Invalid parameters to CopyObjectAndChildren");

    CancellationToken.ThrowIfCancellationRequested();

    using ParallelServerApi api = new(BaseUri, AuthorizationToken, BlobStorageFolder, TimeoutSeconds);

      var stopwatch = Stopwatch.StartNew();
      api.CancellationToken = CancellationToken;
      try
      {
        string rootObjectJson = await api.DownloadSingleObject(StreamId, id).ConfigureAwait(false);
        List<string> allIds = ParseChildrenIds(rootObjectJson);

        List<string> childrenIds = allIds.Where(id => !id.Contains("blob:")).ToList();
        List<string> blobIds = allIds.Where(id => id.Contains("blob:")).Select(id => id.Remove(0, 5)).ToList();

        onTotalChildrenCountKnown?.Invoke(allIds.Count);

        //
        // Objects download
        //

        // Check which children are not already in the local transport
        var childrenFoundMap = await targetTransport.HasObjects(childrenIds).ConfigureAwait(false);
        List<string> newChildrenIds =
          new(from objId in childrenFoundMap.Keys where !childrenFoundMap[objId] select objId);

        targetTransport.BeginWrite();

        await api.DownloadObjects(
            StreamId,
            newChildrenIds,
            (id, json) =>
            {
              stopwatch.Stop();
              targetTransport.SaveObject(id, json);
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
          .Where(id => !localBlobTrimmedHashes.Contains(id.Substring(0, Blob.LocalHashPrefixLength)))
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
      catch (Exception e)
      {
        OnErrorAction?.Invoke(TransportName, e);
        return null;
      }
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

  public async Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
  {
    if (string.IsNullOrEmpty(StreamId) || objectIds == null)
      throw new Exception("Invalid parameters to HasObjects");
    return await Api.HasObjects(StreamId, objectIds).ConfigureAwait(false);
  }

  public void SaveObject(string id, string serializedObject)
  {
    if (string.IsNullOrEmpty(StreamId) || string.IsNullOrEmpty(id) || serializedObject == null)
      throw new Exception("Invalid parameters to SaveObject");
    lock (SendBufferLock)
    {
      if (ErrorState)
        return;
      SendBuffer.Add((id, serializedObject));
      IsWriteComplete = false;
    }
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    if (string.IsNullOrEmpty(StreamId) || string.IsNullOrEmpty(id) || sourceTransport == null)
      throw new InvalidOperationException("Invalid parameters to SaveObject");
    SaveObject(id, sourceTransport.GetObject(id));
  }

  public void BeginWrite()
  {
    if (ShouldSendThreadRun || SendingThread != null)
      throw new InvalidOperationException("ServerTransport already sending");
    TotalSentBytes = 0;
    SavedObjectCount = 0;

    ErrorState = false;
    ShouldSendThreadRun = true;
    SendingThread = new Thread(SendingThreadMain);
    SendingThread.Name = "ServerTransportSender";
    SendingThread.IsBackground = true;
    SendingThread.Start();
  }

  public async Task WriteComplete()
  {
    while (true)
    {
      lock (SendBufferLock)
        if (IsWriteComplete || ErrorState)
          return;
      await Task.Delay(50).ConfigureAwait(false);
    }
  }

  public void EndWrite()
  {
    if (!ShouldSendThreadRun || SendingThread == null)
      throw new InvalidOperationException("ServerTransport not sending");
    ShouldSendThreadRun = false;
    SendingThread.Join();
    SendingThread = null;
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

  private List<string> ParseChildrenIds(string json)
  {
    List<string> childrenIds = new();
    try
    {
      JObject doc1 = JObject.Parse(json);
      foreach (JToken prop in doc1["__closure"])
        childrenIds.Add(((JProperty)prop).Name);
    }
    catch
    {
      // empty children list if no __closure key is found
    }
    return childrenIds;
  }

  private async void SendingThreadMain()
  {
    while (true)
    {
      var stopwatch = Stopwatch.StartNew();
      if (!ShouldSendThreadRun || CancellationToken.IsCancellationRequested)
        return;
      List<(string, string)> buffer = null;
      lock (SendBufferLock)
        if (SendBuffer.Count > 0)
        {
          buffer = SendBuffer;
          SendBuffer = new List<(string, string)>();
        }
        else
        {
          IsWriteComplete = true;
        }

      if (buffer == null)
      {
        Thread.Sleep(100);
        continue;
      }
      try
      {
        List<(string, string)> bufferObjects = buffer.Where(tuple => !tuple.Item1.Contains("blob")).ToList();
        List<(string, string)> bufferBlobs = buffer.Where(tuple => tuple.Item1.Contains("blob")).ToList();

        List<string> objectIds = new(bufferObjects.Count);

        foreach ((string id, _) in bufferObjects)
          if (id != "blob")
            objectIds.Add(id);

        Dictionary<string, bool> hasObjects = await Api.HasObjects(StreamId, objectIds).ConfigureAwait(false);
        List<(string, string)> newObjects = new();
        foreach ((string id, object json) in bufferObjects)
          if (!hasObjects[id])
            newObjects.Add((id, json as string));

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
        OnErrorAction?.Invoke(TransportName, ex);
        lock (SendBufferLock)
        {
          SendBuffer.Clear();
          ErrorState = true;
        }
        return;
      }
      finally
      {
        stopwatch.Stop();
        lock (ElapsedLock)
          Elapsed += stopwatch.Elapsed;
      }
    }
  }
}
