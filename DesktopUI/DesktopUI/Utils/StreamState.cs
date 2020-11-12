using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Stylet;

namespace Speckle.DesktopUI.Utils
{
  /// <summary>
  /// This is the representation of each individual stream "card" that gets saved to the
  /// project file. It includes the stream itself, the object filter method, and basic
  /// account information so a `Client` can be recreated.
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public partial class StreamState : PropertyChangedBase
  {
    public StreamState()
    {
    }

    public StreamState(Client client, Stream stream)
    {
      Client = client;
      Stream = stream;
    }

    /// <summary>
    /// Recreates the client when the state is deserialised.
    /// If the account doesn't exist, it tries to find another account on the same server.
    /// </summary>
    /// <param name="accountId"></param>
    [JsonConstructor]
    public StreamState(string accountId)
    {
      var account = AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId) ??
                    AccountManager.GetAccounts().FirstOrDefault(a => a.serverInfo.url == ServerUrl);
      if (account == null) return;

      Client = new Client(account);
    }

    private Client _client;

    public Client Client
    {
      get => _client;
      set
      {
        if (value.AccountId == null) return;
        _client = value;
        AccountId = Client.AccountId;
        ServerUrl = Client.ServerUrl;
      }
    }

    [JsonProperty]
    public string AccountId { get; private set; }

    [JsonProperty]
    public string ServerUrl { get; private set; }

    private bool _IsSender = true;

    /// <summary>
    /// Tells us wether this is a receiver or a sender card.
    /// </summary>
    [JsonProperty]
    public bool IsSenderCard
    {
      get => _IsSender;
      set
      {
        SetAndNotify(ref _IsSender, value);
      }
    }

    public bool IsReceiverCard
    {
      get => !_IsSender;
    }

    private Stream _stream;
    [JsonProperty]
    public Stream Stream
    {
      get => _stream;
      set
      {
        SetAndNotify(ref _stream, value);
        Initialise();
      }
    }

    private Branch _Branch;
    [JsonProperty]
    public Branch Branch
    {
      get => _Branch;
      set { SetAndNotify(ref _Branch, value); }
    }

    private ISelectionFilter _filter;

    public ISelectionFilter Filter
    {
      get => _filter;
      set => SetAndNotify(ref _filter, value);
    }

    private List<Base> _placeholders = new List<Base>();

    [JsonProperty]
    public List<Base> Placeholders
    {
      get => _placeholders;
      set => SetAndNotify(ref _placeholders, value);
    }

    private List<Base> _objects = new List<Base>();

    [JsonProperty]
    public List<Base> Objects
    {
      get => _objects;
      set => SetAndNotify(ref _objects, value);
    }

    private ProgressReport _progress = new ProgressReport();

    public ProgressReport Progress
    {
      get => _progress;
      set => SetAndNotify(ref _progress, value);
    }

    private bool _isSending;

    public bool IsSending
    {
      get => _isSending;
      set => SetAndNotify(ref _isSending, value);
    }

    private bool _isReceiving;

    public bool IsReceiving
    {
      get => _isReceiving;
      set => SetAndNotify(ref _isReceiving, value);
    }

    private bool _serverUpdates;

    public bool ServerUpdates
    {
      get => _serverUpdates;
      set => SetAndNotify(ref _serverUpdates, value);
    }

    public CancellationToken CancellationToken { get; set; }

    internal void Initialise()
    {
      if (Stream == null || Client?.AccountId == null) return;

      Client.SubscribeStreamUpdated(Stream.id);
      Client.SubscribeCommitCreated(Stream.id);
      Client.SubscribeCommitUpdated(Stream.id);
      Client.SubscribeCommitDeleted(Stream.id);

      Client.OnStreamUpdated += HandleStreamUpdated;
      Client.OnCommitCreated += HandleCommitCreated;
      Client.OnCommitDeleted += HandleCommitCreated;
      Client.OnCommitUpdated += HandleCommitChanged;

      if (Branch == null)
      {
        Branch = Stream.branches.items[0];
      }
    }

    private void HandleStreamUpdated(object sender, StreamInfo info)
    {
      Stream.name = info.name;
      Stream.description = info.description;
      NotifyOfPropertyChange(nameof(Stream));
    }

    private void HandleCommitCreated(object sender, CommitInfo info)
    {
      if (LatestCommit().id == info.id) return;
      ServerUpdates = true;
    }

    private void HandleCommitChanged(object sender, CommitInfo info)
    {
      var branch = Stream.branches.items.FirstOrDefault(b => b.name == info.branchName);
      var commit = branch?.commits.items.FirstOrDefault(c => c.id == info.id);
      if (commit == null)
      {
        // something went wrong, but notify the user there were changes anyway
        // ((look like this sub isn't returning a branch name?))
        ServerUpdates = true;
        return;
      }

      commit.message = info.message;
      NotifyOfPropertyChange(nameof(Stream));
    }

    public Commit LatestCommit(string branchName = "main")
    {
      var branch = Stream.branches.items.Find(b => b.name == branchName);
      if (branch == null)
      {
        Log.CaptureException(new SpeckleException($"Could not find branch {branchName} on stream {Stream.id}"));
        return null;
      }

      var commits = branch.commits.items;
      return commits.Any() ? commits[0] : null;
    }
  }

  public class StreamStateWrapper
  {
    public List<StreamState> StreamStates { get; set; } = new List<StreamState>();

    public List<string> GetStringList()
    {
      var states = new List<string>();
      foreach (var state in StreamStates)
      {
        states.Add(JsonConvert.SerializeObject(state));
      }

      return states;
    }

    public void SetState(IEnumerable<string> stringList)
    {
      StreamStates = stringList.Select(JsonConvert.DeserializeObject<StreamState>).ToList();
    }
  }
}
