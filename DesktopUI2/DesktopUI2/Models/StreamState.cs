using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopUI2.Models
{
  /// <summary>
  /// This is the representation of each individual stream "card" that gets saved to the
  /// project file. It includes the stream itself, the object filter method, and basic
  /// account information so a `Client` can be recreated.
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public class StreamState
  {
    private Client _client;

    public Client Client
    {
      get
      {
        if (_client == null)
        {
          var account = AccountManager.GetAccounts(ServerUrl).FirstOrDefault(x => x.userInfo.id == UserId);
          if (account != null)
            _client = new Client(account);
        }
        return _client;
      }
      set
      {
        _client = value;
        UserId = Client.Account.userInfo.id;
        ServerUrl = Client.ServerUrl;
      }
    }

    private Stream cachedStream;

    [JsonProperty]
    public Stream CachedStream
    {
      get
      {
        return cachedStream;
      }

      set
      {
        var stream = value;
        //acad fix
        //if (stream != null)
        //{
        //  stream.collaborators = new List<Collaborator>();
        //}
        cachedStream = stream;
      }
    }

    /// <summary>
    /// Note: this is not the StreamId, it's a unique identifier for cached streams
    /// </summary>
    [JsonProperty]
    public string Id { get; private set; }

    [JsonProperty]
    public string UserId { get; private set; }

    [JsonProperty]
    public string ServerUrl { get; private set; }

    [JsonProperty]
    public bool IsReceiver { get; set; }

    [JsonProperty]
    public bool AutoReceive { get; set; }

    [JsonProperty]
    public ReceiveMode ReceiveMode { get; set; }

    [JsonProperty]
    public string StreamId { get; set; }

    /// <summary>
    /// The Selected Branch 
    /// </summary>
    [JsonProperty]
    public string BranchName { get; set; }

    /// <summary>
    /// The selected Commit ID or "latest"
    /// </summary>
    [JsonProperty]
    public string CommitId { get; set; }


    /// <summary>
    /// The selected Commit ID or "latest"
    /// </summary>
    [JsonProperty]
    public string ReferencedObject { get; set; }

    /// <summary>
    /// Last time the stream card was used to receive or send
    /// </summary>
    [JsonProperty]
    public string LastUsed { get; set; }


    /// <summary>
    /// Allows clients to keep track of the previous commit id they created, and propagate it to the next one.
    /// </summary>
    [JsonProperty]
    public string PreviousCommitId { get; set; }

    [JsonProperty]
    public string CommitMessage { get; set; }

    [JsonProperty]
    public bool SchedulerEnabled { get; set; }

    [JsonProperty]
    public string SchedulerTrigger { get; set; }

    [JsonProperty]
    [JsonConverter(typeof(SelectionFilterConverter))]
    public ISelectionFilter Filter { get; set; }

    public List<ISetting> Settings { get; set; } = new List<ISetting>();

    //List of uniqueids of the currently selected objects
    //the values are updated only upon sending
    [JsonProperty]
    public List<string> SelectedObjectIds { get; set; } = new List<string>();

    [JsonProperty]
    public List<ApplicationPlaceholderObject> ReceivedObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public StreamState(StreamAccountWrapper streamAccountWrapper)
    {
      Init(streamAccountWrapper.Account, streamAccountWrapper.Stream);
    }
    public StreamState(Account account, Stream stream)
    {
      Init(account, stream);
    }

    public void Init(Account account, Stream stream)
    {
      Client = new Client(account);
      StreamId = stream.id;
      CachedStream = stream;
      Id = Guid.NewGuid().ToString();
    }

    public StreamState()
    {

    }
  }
}
