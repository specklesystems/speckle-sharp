using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.Core.Transports
{
  public class ServerTransportV2 : IDisposable, ICloneable, ITransport
  {
    public string TransportName { get; set; } = "RemoteTransport";
    public CancellationToken CancellationToken { get; set; }
    public Action<string, int> OnProgressAction { get; set; }
    public Action<string, Exception> OnErrorAction { get; set; }

    public Account Account { get; set; }
    public string BaseUri { get; private set; }
    public string StreamId { get; set; }
    private HttpClient Client { get; set; }

    public ServerTransportV2(Account account, string streamId, int timeoutSeconds = 60)
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
    }

    public async Task<string> CopyObjectAndChildren(string id, ITransport targetTransport, Action<int> onTotalChildrenCountKnown = null)
    {
      if (CancellationToken.IsCancellationRequested)
        return null;

      // Get root object
      var rootHttpMessage = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}/{id}/single", UriKind.Relative),
        Method = HttpMethod.Get,
      };
      HttpResponseMessage rootHttpResponse = null;
      try
      {
        rootHttpResponse = await Client.SendAsync(rootHttpMessage, HttpCompletionOption.ResponseContentRead, CancellationToken);
        rootHttpResponse.EnsureSuccessStatusCode();
      }
      catch (Exception e)
      {
        OnErrorAction?.Invoke(TransportName, e);
        return null;
      }

      // Parse children ids
      String rootObjectStr = await rootHttpResponse.Content.ReadAsStringAsync();
      List<string> childrenIds = new List<string>();
      try
      {
        using (JsonDocument doc = JsonDocument.Parse(rootObjectStr))
        {
          JsonElement closures = doc.RootElement.GetProperty("__closure");
          foreach (JsonProperty prop in closures.EnumerateObject())
            childrenIds.Add(prop.Name);
        }
      }
      catch
      {
        // empty children list if no __closure key is found
      }
      onTotalChildrenCountKnown?.Invoke(childrenIds.Count);

      // Check which children are not already in the local transport
      var childrenFoundMap = await targetTransport.HasObjects(childrenIds);
      List<string> newChildrenIds = new List<string>(from objId in childrenFoundMap.Keys where !childrenFoundMap[objId] select objId);

      targetTransport.BeginWrite();

      // Get the children that are not already in the targetTransport
      List<string> childrenIdBatch = new List<string>(DOWNLOAD_BATCH_SIZE);
      bool downloadBatchResult;
      foreach (var objectId in newChildrenIds)
      {
        childrenIdBatch.Add(objectId);
        if (childrenIdBatch.Count >= DOWNLOAD_BATCH_SIZE)
        {
          downloadBatchResult = await CopyObjects(childrenIdBatch, targetTransport);
          if (!downloadBatchResult)
            return null;
          childrenIdBatch = new List<string>(DOWNLOAD_BATCH_SIZE);
        }
      }
      if (childrenIdBatch.Count > 0)
      {
        downloadBatchResult = await CopyObjects(childrenIdBatch, targetTransport);
        if (!downloadBatchResult)
          return null;
      }

      targetTransport.SaveObject(hash, rootObjectStr);
      await targetTransport.WriteComplete();
      return rootObjectStr;
    }

    public string GetObject(string id)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return null;
      }

      var message = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/objects/{StreamId}/{id}/single", UriKind.Relative),
        Method = HttpMethod.Get,
      };

      var response = Client.SendAsync(message, HttpCompletionOption.ResponseContentRead, CancellationToken).Result.Content;
      return response.ReadAsStringAsync().Result;
    }

    public Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
    {
      throw new NotImplementedException();
    }

    public void SaveObject(string id, string serializedObject)
    {
      throw new NotImplementedException();
    }

    public void SaveObject(string id, ITransport sourceTransport)
    {
      throw new NotImplementedException();
    }

    public void BeginWrite()
    {
      
    }

    public Task WriteComplete()
    {
      List<Task> pendingTasks = new List<Task>();
      return Task.WhenAll(pendingTasks);
    }

    public void EndWrite()
    {
      
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

    public void Dispose()
    {
      Client?.Dispose();
    }
  }
}
