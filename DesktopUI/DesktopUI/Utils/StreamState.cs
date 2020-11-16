using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Stylet;
using static Speckle.DesktopUI.Utils.BranchContextMenuItem;

namespace Speckle.DesktopUI.Utils
{
  /// <summary>
  /// This is the representation of each individual stream "card" that gets saved to the
  /// project file. It includes the stream itself, the object filter method, and basic
  /// account information so a `Client` can be recreated.
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public partial class StreamState : PropertyChangedBase, IHandle<UpdateSelectionCountEvent>
  {
    private Client _client;
    public Client Client
    {
      get => _client;
      set
      {
        if (value.AccountId == null)
        {
          return;
        }

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
        NotifyOfPropertyChange(nameof(BranchContextMenuItems));
      }
    }
    public bool IsReceiverCard
    {
      get => !_IsSender;
    }

    private Stream _stream;
    /// <summary>
    /// Setting this property will re-initialise this class.
    /// </summary>
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
      set
      {
        SetAndNotify(ref _Branch, value);

        if (value.commits != null && value.commits.items != null && value.commits.items.Count != 0)
        {
          Commit = value.commits.items[0];
        }
        else
        {
          Commit = new Commit { id = "Empty Branch" };
        }

        NotifyOfPropertyChange(nameof(BranchContextMenuItems));
      }
    }

    public BindableCollection<BranchContextMenuItem> BranchContextMenuItems
    {
      get
      {
        var all = new BindableCollection<BranchContextMenuItem>();
        all.AddRange(
          Stream.branches.items.Select(b => new BranchContextMenuItem()
          {
            Branch = b,
            Tooltip = Branch.name == b.name ? "Current branch" : $"Switch to {b.name}",
            Icon = Branch.name == b.name ? new PackIcon { Kind = PackIconKind.CheckBold, FontSize = 12 } : new PackIcon { Kind = PackIconKind.SourceBranch, FontSize = 12 },
            CommandArgument = new BranchSwitchCommandArgument { RootStreamState = this, Branch = b }
          }));;

        if (IsSenderCard)
        {
          all.Add(new BranchContextMenuItem()
          {
            Branch = new Branch { name = "Add a new branch" },
            Tooltip = "Adds a new branch and sets it.",
            Icon = new PackIcon { Kind = PackIconKind.Add, FontSize = 12 },
            CommandArgument = new BranchSwitchCommandArgument { RootStreamState = this, Branch = new Branch { name = "Add a new branch" } }
          });
        }

        return all;
      }
    }

    private Commit _Commit;
    [JsonProperty]
    public Commit Commit
    {
      get => _Commit;
      set
      {
        SetAndNotify(ref _Commit, value);
        NotifyOfPropertyChange(nameof(CommitContextMenuItems));
        NotifyOfPropertyChange(nameof(ReceiveEnabled));
        NotifyOfPropertyChange(nameof(ReceiveDisabled));
      }
    }

    public BindableCollection<CommitContextMenuItem> CommitContextMenuItems
    {
      get
      {
        var collection = new BindableCollection<CommitContextMenuItem>() { };

        if (Branch.commits == null || Branch.commits.items == null || Branch.commits.items.Count == 0)
        {
          collection.Add(new CommitContextMenuItem
          {
            Icon = new PackIcon { Kind = PackIconKind.Warning },
            MainText = "This branch has no commits.",
            SecondaryText = "Try switching to a different branch.",
            ToolTip = "Nada. It's empty",
            CommandArgument = new CommitContextMenuItem.CommitSwitchCommandArgument { RootStreamState = this, Commit = null }
          });

          return collection;
        }

        collection.AddRange(Branch.commits.items.Select(commit => new CommitContextMenuItem
        {
          MainText = commit.message,
          SecondaryText = $"by {commit.authorName} - {Formatting.TimeAgo(commit.createdAt)}",
          ToolTip = commit.message,
          Icon = Commit.id == commit.id ? new PackIcon { Kind = PackIconKind.Check } : new PackIcon { Kind = PackIconKind.SourceCommit },
          CommandArgument = new CommitContextMenuItem.CommitSwitchCommandArgument { RootStreamState = this, Commit = commit, }
        }));

        return collection;
      }
    }

    private ISelectionFilter _filter;
    [JsonProperty]
    public ISelectionFilter Filter
    {
      get => _filter;
      set
      {
        SetAndNotify(ref _filter, value);
        NotifyOfPropertyChange(nameof(SendEnabled));
        NotifyOfPropertyChange(nameof(ObjectSelectionButtonText));
        NotifyOfPropertyChange(nameof(ObjectSelectionTooltipText));
        NotifyOfPropertyChange(nameof(ObjectSelectionButtonIcon));
      }
    }

    public bool AppHasFilters
    {
      get => Globals.HostBindings.GetSelectionFilters().Count != 0;
    }

    private int _selectionCount = 0;
    public int SelectionCount
    {
      get => _selectionCount;
      set => SetAndNotify(ref _selectionCount, value);
    }

    public bool SendEnabled
    {
      get => Objects.Count != 0 || Filter != null;
    }

    public bool SendDisabled
    {
      get => !SendEnabled;
    }

    public bool ReceiveEnabled
    {
      get => Commit != null && Commit.id != "Empty Branch";
    }

    public bool ReceiveDisabled
    {
      get => !ReceiveEnabled;
    }

    public string ObjectSelectionButtonText
    {
      get
      {
        if (Filter != null)
        {
          return $"{Filter.Summary}";
        }
        return $"{Objects.Count} objects";
      }
    }

    public string ObjectSelectionTooltipText
    {
      get
      {
        if (Filter != null)
        {
          return $"Current filter is by {Filter.Name}: {Filter.Summary}";
        }
        else
        {
          return $"Current object selection: {Objects.Count}.";
        }
      }
    }

    public PackIcon ObjectSelectionButtonIcon
    {
      get
      {
        if (Filter != null)
        {
          return new PackIcon { Kind = (PackIconKind)Enum.Parse(typeof(PackIconKind), Filter.Icon) };
        }
        else if (Objects.Count == 0)
        {
          return new PackIcon { Kind = PackIconKind.CubeOutline };
        }
        else
        {
          return new PackIcon { Kind = PackIconKind.Cube };
        }
      }
    }

    private List<Base> _objects = new List<Base>();
    [JsonProperty]
    public List<Base> Objects
    {
      get => _objects;
      set
      {
        SetAndNotify(ref _objects, value);
        NotifyOfPropertyChange(nameof(ObjectSelectionTooltipText));
        NotifyOfPropertyChange(nameof(ObjectSelectionButtonText));
        NotifyOfPropertyChange(nameof(SelectionCount));
        NotifyOfPropertyChange(nameof(SendEnabled));
        NotifyOfPropertyChange(nameof(SendDisabled));
      }
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
      set { SetAndNotify(ref _isSending, value); }
    }

    private bool _isReceiving;
    public bool IsReceiving
    {
      get => _isReceiving;
      set
      {
        SetAndNotify(ref _isReceiving, value);
      }
    }

    private bool _serverUpdates;
    public bool ServerUpdates
    {
      get => _serverUpdates;
      set
      {
        SetAndNotify(ref _serverUpdates, value);
      }
    }

    private bool _showProgressBar = false;
    public bool ShowProgressBar
    {
      get => _showProgressBar;
      set
      {
        SetAndNotify(ref _showProgressBar, value);
      }
    }

    public bool _ProgressBarIsIndeterminate = false;
    public bool ProgressBarIsIndeterminate
    {
      get => _ProgressBarIsIndeterminate;
      set
      {
        SetAndNotify(ref _ProgressBarIsIndeterminate, value);
      }
    }

    public CancellationTokenSource CancellationTokenSource { get; set; }

    private string _CommitMessage;
    public string CommitMessage
    {
      get => _CommitMessage;
      set
      {
        SetAndNotify(ref _CommitMessage, value);
      }
    }

    #region constructors

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
      if (account == null)
      {
        // TODO : Notify error!
        return;
      }

      Client = new Client(account);
    }

    internal void Initialise()
    {
      if (Stream == null || Client?.AccountId == null)
      {
        return;
      }

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
        var tempBranch = Stream.branches.items.FirstOrDefault(b => b.name == "main");
        if (tempBranch == null)
        {
          Branch = Stream.branches.items[0];
        }
        else
        {
          Branch = tempBranch;
        }
      }

      if (Branch.commits != null && Branch.commits.items != null && Branch.commits.items.Count != 0)
      {
        Commit = Branch.commits.items[0];
      }
      else
      {
        Commit = new Commit { id = "No Commits" };
      }
    }

    #endregion

    #region Main Actions

    // Used for branch names; ignore
    private Random rnd = new Random();

    public void SwitchBranch(Branch branch)
    {
      if (branch == null)
      {
        Stream.branches.items.Add(new Branch
        {
          name = "TODO_" + rnd.Next(1, 10).ToString(),
          id = rnd.Next(1, 1000000).ToString()
        });
        Branch = Stream.branches.items.Last();

        NotifyOfPropertyChange(nameof(BranchContextMenuItem));
        Globals.Notify($"Created branch {Branch.name} and switched to it.");
        return;
      }

      Branch = branch;
      Globals.Notify($"Switched active branch to {Branch.name}.");
      NotifyOfPropertyChange(nameof(BranchContextMenuItem));
    }

    public void SwitchCommit(Commit commit)
    {
      if (commit == null)
      {
        return;
      }

      Commit = commit;
    }

    public void SendWithCommitMessage(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        Send();
      }
    }

    public async void Send()
    {
      if (IsSending || IsReceiving)
      {
        Globals.Notify("Operation in progress. Cannot send at this time.");
        return;
      }

      Tracker.TrackPageview(Tracker.SEND);
      IsSending = true;
      ShowProgressBar = true;
      ProgressBarIsIndeterminate = false;
      CancellationTokenSource = new CancellationTokenSource();

      await Task.Run(() => Globals.Repo.ConvertAndSend(this));

      ShowProgressBar = false;
      Progress.ResetProgress();
      CommitMessage = null;
      IsSending = false;
      Globals.Notify($"Data uploded to {Stream.name}!");
    }

    public async void Receive()
    {
      if (IsSending || IsReceiving)
      {
        Globals.Notify("Operation in progress. Cannot send at this time.");
        return;
      }

      Tracker.TrackPageview(Tracker.RECEIVE);

      IsReceiving = true;
      ShowProgressBar = true;
      ProgressBarIsIndeterminate = false;
      CancellationTokenSource = new CancellationTokenSource();

      await Task.Run(() => Globals.Repo.ConvertAndReceive(this));

      ShowProgressBar = false;
      Progress.ResetProgress();
      IsReceiving = false;
      Globals.Notify($"Data received from {Stream.name}!");
    }

    public void CancelSendOrReceive()
    {
      CancellationTokenSource.Cancel();
      ProgressBarIsIndeterminate = true;
    }

    public void SwapState()
    {
      IsSenderCard = !IsSenderCard;

    }

    #endregion

    #region Selection events

    public void SetObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      Objects = objIds.Select(id => new Base { applicationId = id }).ToList();

      Globals.Notify("Object selection set.");
      Filter = null;
    }

    public void AddObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      objIds.ForEach(id =>
      {
        if (Objects.FirstOrDefault(b => b.applicationId == id) == null)
        {
          Objects.Add(new Base { applicationId = id });
        }
      });

      Globals.Notify("Object added.");
      Filter = null;
    }

    public void RemoveObjectSelection()
    {
      var objIds = Globals.HostBindings.GetSelectedObjects();
      if (objIds == null || objIds.Count == 0)
      {
        Globals.Notify("Could not get object selection.");
        return;
      }

      var filtered = Objects.Where(o => objIds.IndexOf(o.applicationId) == -1).ToList();

      if (filtered.Count == Objects.Count)
      {
        Globals.Notify("No objects removed.");
        return;
      }

      Globals.Notify($"{Objects.Count - filtered.Count} objects removed.");
      Objects = filtered;
    }

    public void ClearObjectSelection()
    {
      Objects = new List<Base>();
      Filter = null;
      Globals.Notify($"Selection cleared.");
    }

    #endregion

    #region application events 

    private void HandleStreamUpdated(object sender, StreamInfo info)
    {
      Stream.name = info.name;
      Stream.description = info.description;
      NotifyOfPropertyChange(nameof(Stream));
    }

    public void Handle(UpdateSelectionCountEvent message)
    {
      SelectionCount = message.SelectionCount;
    }

    private void HandleCommitCreated(object sender, CommitInfo info)
    {
      if (LatestCommit().id == info.id)
      {
        return;
      }

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

    #endregion

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

  /// <summary>
  /// Class used for handling the actions around the context menu of branches (sender/receiver),.
  /// </summary>
  public class BranchContextMenuItem
  {
    public Branch Branch { get; set; }
    public string Tooltip { get; set; }
    public PackIcon Icon { get; set; }
    public BranchSwitchCommandArgument CommandArgument { get; set; }

    public class BranchSwitchCommandArgument
    {
      public StreamState RootStreamState { get; set; }
      public Branch Branch { get; set; }
    }
  }


  /// <summary>
  /// Class used for handling actions around the context menu of the commit switcher (receiver).
  /// </summary>
  public class CommitContextMenuItem
  {
    public string MainText { get; set; }
    public string SecondaryText { get; set; }
    public string ToolTip { get; set; }
    public PackIcon Icon { get; set; } = new PackIcon { Kind = PackIconKind.SourceCommit };
    public CommitSwitchCommandArgument CommandArgument { get; set; }

    public class CommitSwitchCommandArgument
    {
      public StreamState RootStreamState { get; set; }
      public Commit Commit { get; set; }
    }
  }
}
