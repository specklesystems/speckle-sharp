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
  public class StreamState
  {
    private string branchName = "main";
    private Client _client;

    public bool IsSending { get; set; }
    public bool IsReceiving { get; set; }

    public Speckle.Core.Api.Stream Stream { get; set; }
    
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

    public StreamState()
    {

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

    public async Task<bool> RefreshStream()
    {
      try
      {
        var updatedStream = await Client.StreamGet(Stream.id);
        Stream.name = updatedStream.name;
        Stream.description = updatedStream.description;
        Stream.isPublic = updatedStream.isPublic;
      }
      catch (Exception)
      {
        return false;
      }

      return true;
    }

  }
}
