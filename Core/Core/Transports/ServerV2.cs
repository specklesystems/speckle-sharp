using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports.ServerUtils;
using Speckle.Newtonsoft.Json.Linq;

namespace Speckle.Core.Transports
{
  public class ServerTransport : ServerTransportV2
  {
    public ServerTransport(Account account, string streamId, int timeoutSeconds = 60) : base(account, streamId, timeoutSeconds)
    {
    }
  }

  public class ServerTransportV2 : IDisposable, ICloneable, ITransport, IBlobCapableTransport
  {
    public string TransportName { get; set; } = "RemoteTransport";
    public CancellationToken CancellationToken { get; set; }
    public Action<string, int> OnProgressAction { get; set; }
    public Action<string, Exception> OnErrorAction { get; set; }

    public int TotalSentBytes { get; set; } = 0;
    public int SavedObjectCount { get; private set; } = 0;

    public Account Account { get; set; }
    public string BaseUri { get; private set; }
    public string StreamId { get; set; }

    public int TimeoutSeconds { get; set; }
    private string AuthorizationToken { get; set; }

    public ParallelServerApi Api { get; private set; }

    public string BlobStorageFolder { get; set; }

    private bool ShouldSendThreadRun = false;
    private bool IsWriteComplete = false;
    private Thread SendingThread = null;
    private object SendBufferLock = new object();

    // TODO: make send buffer more flexible to accept blobs too, otherwise we'll be computing blob hashes twice
    private List<(string, string)> SendBuffer = new List<(string, string)>();

    private bool ErrorState = false;

    public ServerTransportV2(Account account, string streamId, int timeoutSeconds = 60, string blobStorageFolder = null)
    {
      Account = account;
      CancellationToken = CancellationToken.None;
      Initialize(account.serverInfo.url, streamId, account.token, timeoutSeconds);

      if(blobStorageFolder == null)
      {
        BlobStorageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blobs");
      }
      Directory.CreateDirectory(BlobStorageFolder);
    }

    private void Initialize(string baseUri, string streamId, string authorizationToken, int timeoutSeconds = 60)
    {
      Log.AddBreadcrumb("New Remote Transport");

      BaseUri = baseUri;
      StreamId = streamId;
      AuthorizationToken = authorizationToken;
      TimeoutSeconds = timeoutSeconds;

      Api = new ParallelServerApi(BaseUri, AuthorizationToken, TimeoutSeconds);
      Api.OnBatchSent = (num, size) => {
        OnProgressAction?.Invoke(TransportName, num);
        TotalSentBytes += size;
        SavedObjectCount += num;
      };
    }

    public async Task<string> CopyObjectAndChildren(string id, ITransport targetTransport, Action<int> onTotalChildrenCountKnown = null)
    {
      if (String.IsNullOrEmpty(StreamId) || String.IsNullOrEmpty(id) || targetTransport == null)
        throw new Exception("Invalid parameters to CopyObjectAndChildren");

      if (CancellationToken.IsCancellationRequested)
        return null;

      using(ParallelServerApi api = new ParallelServerApi(BaseUri, AuthorizationToken, TimeoutSeconds))
      {
        api.CancellationToken = CancellationToken;
        try
        {
          string rootObjectJson = await api.DownloadSingleObject(StreamId, id);
          List<string> allIds = ParseChildrenIds(rootObjectJson);

          List<string> childrenIds = allIds.Where(id => !id.Contains("blob:")).ToList();
          List<string> blobIds = allIds.Where(id => id.Contains("blob:")).ToList();

          onTotalChildrenCountKnown?.Invoke(childrenIds.Count);

          // Check which children are not already in the local transport
          var childrenFoundMap = await targetTransport.HasObjects(childrenIds);
          List<string> newChildrenIds = new List<string>(from objId in childrenFoundMap.Keys where !childrenFoundMap[objId] select objId);

          targetTransport.BeginWrite();

          await api.DownloadObjects(StreamId, newChildrenIds, (string id, string json) =>
          {
            targetTransport.SaveObject(id, json);
            OnProgressAction?.Invoke(TransportName, 1);
          });

          targetTransport.SaveObject(id, rootObjectJson);

          await targetTransport.WriteComplete();
          targetTransport.EndWrite();

          return rootObjectJson;
        }
        catch (Exception e)
        {
          OnErrorAction?.Invoke(TransportName, e);
          return null;
        }
      }

    }

    public string GetObject(string id)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return null;
      }
      return Api.DownloadSingleObject(StreamId, id).Result;
    }

    public async Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
    {
      if (String.IsNullOrEmpty(StreamId) || objectIds == null)
        throw new Exception("Invalid parameters to HasObjects");
      return await Api.HasObjects(StreamId, objectIds);
    }

    public void SaveObject(string id, string serializedObject)
    {
      if (String.IsNullOrEmpty(StreamId) || String.IsNullOrEmpty(id) || serializedObject == null)
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
      if (String.IsNullOrEmpty(StreamId) || String.IsNullOrEmpty(id) || sourceTransport == null)
        throw new Exception("Invalid parameters to SaveObject");
      SaveObject(id, sourceTransport.GetObject(id));
    }

    public void SaveBlob(Blob obj)
    {
      if (String.IsNullOrEmpty(StreamId) || obj == null)
        throw new Exception("Invalid parameters to SaveBlob");
      var hash = obj.GetFileHash();

      lock (SendBufferLock)
      {
        if (ErrorState)
          return;
        SendBuffer.Add(($"blob:{hash}", obj.filePath));
      }
    }

    public void GetBlob(Blob obj)
    {
      throw new NotImplementedException();
    }

    public void BeginWrite()
    {
      if (ShouldSendThreadRun || SendingThread != null)
        throw new Exception("ServerTransport already sending");
      TotalSentBytes = 0;
      SavedObjectCount = 0;

      ErrorState = false;
      ShouldSendThreadRun = true;
      SendingThread = new Thread(new ThreadStart(SendingThreadMain));
      SendingThread.Name = "ServerTransportSender";
      SendingThread.IsBackground = true;
      SendingThread.Start();
    }

    public async Task WriteComplete()
    {
      while (true)
      {
        lock(SendBufferLock)
        {
          if (IsWriteComplete || ErrorState)
            return;
        }
        await Task.Delay(50);
      }
    }

    public void EndWrite()
    {
      if (!ShouldSendThreadRun || SendingThread == null)
        throw new Exception("ServerTransport not sending");
      ShouldSendThreadRun = false;
      SendingThread.Join();
      SendingThread = null;
    }

    public override string ToString()
    {
      return $"Server Transport @{Account.serverInfo.url}";
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

    private List<string> ParseChildrenIds(string json)
    {
      List<string> childrenIds = new List<string>();
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
        if (!ShouldSendThreadRun || CancellationToken.IsCancellationRequested)
        {
          return;
        }
        List<(string, string)> buffer = null;
        lock (SendBufferLock)
        {
          if (SendBuffer.Count > 0)
          {
            buffer = SendBuffer;
            SendBuffer = new List<(string, string)>();
          }
          else
          {
            IsWriteComplete = true;
          }
        }
        if (buffer == null)
        {
          Thread.Sleep(100);
          continue;
        }
        try
        {
          List<(string,string)> bufferObjects = buffer.Where(tuple => !tuple.Item1.Contains("blob")).ToList(); // TODO: probably not efficient, see note on send buffer prop
          List<(string,string)> bufferBlobs = buffer.Where(tuple => tuple.Item1.Contains("blob")).ToList();

          List<string> objectIds = new List<string>(bufferObjects.Count);

          foreach ((string id, string json) in bufferObjects)
          {
            if (id != "blob")
            {
              objectIds.Add(id);
            }
          }

          Dictionary<string, bool> hasObjects = await Api.HasObjects(StreamId, objectIds);
          List<(string, string)> newObjects = new List<(string, string)>();
          foreach ((string id, string json) in bufferObjects)
          {
            if (!hasObjects[id])
            {
              newObjects.Add((id, json));
            }
          }

          // Report the objects that are already on the server
          OnProgressAction?.Invoke(TransportName, hasObjects.Count - newObjects.Count);

          await Api.UploadObjects(StreamId, newObjects);

          // TODO: Has blobs check
          await Api.UploadBlobs(StreamId, bufferBlobs);
        }
        catch(Exception ex)
        {
          OnErrorAction?.Invoke(TransportName, ex);
          lock (SendBufferLock)
          {
            SendBuffer.Clear();
            ErrorState = true;
          }
          return;
        }
      }
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
  }
}
