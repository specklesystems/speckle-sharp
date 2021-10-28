using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectorGSA
{
  [JsonObject(MemberSerialization.OptIn)]
  public class StreamState
  {
    private string branchName = "main";
    private Client _client;

    [JsonProperty]
    public bool IsSending { get; set; }
    [JsonProperty]
    public bool IsReceiving { get; set; }

    public Speckle.Core.Api.Stream Stream { get; set; }

    public List<Exception> Errors { get; set; } = new List<Exception>();

    public Client Client
    {
      get => _client;
      set
      {
        if (value.Account == null)
        {
          return;
        }

        _client = value;
        UserId = Client.Account.userInfo.id;
        ServerUrl = Client.ServerUrl;
      }
    }

    [JsonProperty]
    public string UserId { get; private set; }
    [JsonProperty]
    public string ServerUrl { get; private set; }
    [JsonProperty]
    public string StreamId { get => Stream.id;
      set
      {
        Stream = new Speckle.Core.Api.Stream() { id = value };
      }
    }

    public string Name { get; internal set; }

    public StreamState()
    {

    }

    public bool Equals(StreamState other)
    {
      if (Stream != null && other.Stream != null && Client != null && other.Client != null)
      {
        return ((Stream.id == other.Stream.id) && (IsReceiving == other.IsReceiving) && (IsSending == other.IsSending) 
          && (Client != null && Client.Account != null && Client.Account.userInfo != null)
          && (other.Client != null && other.Client.Account != null && other.Client.Account.userInfo != null)
          && (Client.Account.userInfo.id == other.Client.Account.userInfo.id));
      }
      return false;
    }

    [JsonConstructor]
    public StreamState(string userId, string serverUrl)
    {
      var account = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId || a.id == userId);

      //if the current user is not the one who created the stream, try find an account on that server, and prioritize the default if multiple are found
      if (account == null)
      {
        account = AccountManager.GetAccounts().OrderByDescending(x => x.isDefault).FirstOrDefault(a => a.serverInfo.url == serverUrl);
      }

      if (account == null)
      {
        // TODO : Notify error!
        return;
      }

      Client = new Client(account);
    }

    public async Task<bool> RefreshStream(IProgress<MessageEventArgs> loggingProgress)
    {
      try
      {
        var updatedStream = await Client.StreamGet(Stream.id);
        Stream.name = updatedStream.name;
        Stream.description = updatedStream.description;
        Stream.isPublic = updatedStream.isPublic;
      }
      catch (Exception ex)
      {
        loggingProgress.Report(new MessageEventArgs(Speckle.GSA.API.MessageIntent.Display, Speckle.GSA.API.MessageLevel.Error, "Unable to fetch stream"));
        loggingProgress.Report(new MessageEventArgs(Speckle.GSA.API.MessageIntent.TechnicalLog, Speckle.GSA.API.MessageLevel.Error, ex, "Unable to fetch stream"));
        return false;
      }

      return true;
    }

    public void SetName(string newName)
    {

    }

  }
}
