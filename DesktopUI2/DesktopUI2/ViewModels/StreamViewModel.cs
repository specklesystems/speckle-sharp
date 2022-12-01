using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Threading;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.Views;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Windows.Dialogs;
using DynamicData;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using Stream = Speckle.Core.Api.Stream;

namespace DesktopUI2.ViewModels
{
  public class StreamViewModel : ReactiveObject, IRoutableViewModel, IDisposable
  {

    public StreamState StreamState { get; set; }
    public IScreen HostScreen { get; set; }


    #region bindings

    private ConnectorBindings Bindings;



    private CollaboratorsViewModel Collaborators { get; set; }

    public ICommand RemoveSavedStreamCommand { get; }

    private bool _previewOn = false;
    public bool PreviewOn
    {
      get => _previewOn;
      set
      {
        if (value == false && value != _previewOn)
        {
          if (Progress.IsPreviewProgressing)
            Progress.IsPreviewProgressing = false;
          Bindings.ResetDocument();
        }
        this.RaiseAndSetIfChanged(ref _previewOn, value);
      }
    }
    private Stream _stream;
    public Stream Stream
    {
      get => _stream;
      internal set => this.RaiseAndSetIfChanged(ref _stream, value);
    }

    private ProgressViewModel _progress = new ProgressViewModel();
    public ProgressViewModel Progress
    {
      get => _progress;
      set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    private List<MenuItemViewModel> _menuItems = new List<MenuItemViewModel>();
    public List<MenuItemViewModel> MenuItems
    {
      get => _menuItems;
      private set
      {
        this.RaiseAndSetIfChanged(ref _menuItems, value);
      }
    }

    public string LastUpdated
    {
      get
      {
        return "Updated " + Formatting.TimeAgo(StreamState.CachedStream.updatedAt);
      }
    }

    public string LastUsed
    {
      get
      {
        var verb = StreamState.IsReceiver ? "Received" : "Sent";
        if (StreamState.LastUsed == null)
          return "Never " + verb.ToLower();
        return $"{verb} {Formatting.TimeAgo(StreamState.LastUsed)}";
      }
      set
      {
        StreamState.LastUsed = value;
        this.RaisePropertyChanged("LastUsed");
      }
    }

    private bool _isRemovingStream;
    public bool IsRemovingStream
    {
      get => _isRemovingStream;
      private set
      {
        this.RaiseAndSetIfChanged(ref _isRemovingStream, value);
        this.RaisePropertyChanged("StreamEnabled");
      }
    }

    public bool StreamEnabled
    {
      get => !IsRemovingStream && !NoAccess;
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
      get => _isExpanded;
      private set
      {
        this.RaiseAndSetIfChanged(ref _isExpanded, value);
      }
    }

    private int _selectedTab = 0;
    public int SelectedTab
    {
      get => _selectedTab;
      private set => this.RaiseAndSetIfChanged(ref _selectedTab, value);

    }

    public string UrlPathSegment { get; } = "stream";

    internal Client Client { get; }

    public void GoBack()
    {
      PreviewOn = false;
      Bindings.ResetDocument();
      //if not a saved stream dispose client and subs
      if (!HomeViewModel.Instance.SavedStreams.Any(x => x._guid == _guid))
        Client.Dispose();
      MainViewModel.GoHome();
    }

    //If we don't have access to this stream
    public bool NoAccess { get; set; } = false;

    private bool _isReceiver = false;
    public bool IsReceiver
    {
      get => _isReceiver;
      set
      {
        if (value != _isReceiver)
        {
          PreviewOn = false;
        }
        this.RaiseAndSetIfChanged(ref _isReceiver, value);
        this.RaisePropertyChanged(nameof(BranchesViewModel));
      }
    }

    private bool _autoReceive = false;
    public bool AutoReceive
    {
      get => _autoReceive;
      set
      {
        this.RaiseAndSetIfChanged(ref _autoReceive, value);
      }
    }

    private ReceiveMode _selectedReceiveMode;
    public ReceiveMode SelectedReceiveMode
    {
      get => _selectedReceiveMode;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedReceiveMode, value);
      }
    }

    private BranchViewModel _selectedBranch;
    public BranchViewModel SelectedBranch
    {
      get => _selectedBranch;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedBranch, value);

        if (value == null)
          return;


        if (value.Branch.id == null)
          AddNewBranch();
        else
          GetCommits();


      }
    }

    private List<Branch> _branches;
    public List<Branch> Branches
    {
      get => _branches;
      private set
      {
        this.RaiseAndSetIfChanged(ref _branches, value);
        _branchesViewModel = null;
        this.RaisePropertyChanged(nameof(BranchesViewModel));
      }

    }


    private List<BranchViewModel> _branchesViewModel;
    public List<BranchViewModel> BranchesViewModel
    {
      get
      {
        if (Branches == null)
          return new List<BranchViewModel>();

        if (_branchesViewModel == null)
          _branchesViewModel = Branches.Select(x => new BranchViewModel(x)).ToList();

        //start fresh, just in case
        if (_branchesViewModel.Last().Branch.id == null)
          _branchesViewModel.Remove(_branchesViewModel.Last());

        if (!IsReceiver)
          _branchesViewModel.Add(new BranchViewModel(new Branch { name = "Add New Branch" }, "Plus"));

        return _branchesViewModel;
      }
    }

    private Commit _selectedCommit;
    public Commit SelectedCommit
    {
      get => _selectedCommit;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedCommit, value);
        if (_selectedCommit != null)
        {
          if (_selectedCommit.id == "latest")
            PreviewImageUrl = Client.Account.serverInfo.url + $"/preview/{Stream.id}/branches/{Uri.EscapeDataString(SelectedBranch.Branch.name)}";
          else
            PreviewImageUrl = Client.Account.serverInfo.url + $"/preview/{Stream.id}/commits/{_selectedCommit.id}";
          PreviewImageUrl360 = $"{PreviewImageUrl}/all";
        }
      }
    }

    private List<Commit> _commits = new List<Commit>();
    public List<Commit> Commits
    {
      get => _commits;
      private set => this.RaiseAndSetIfChanged(ref _commits, value);
    }

    private List<ActivityViewModel> _activity;
    public List<ActivityViewModel> Activity
    {
      get => _activity;
      private set => this.RaiseAndSetIfChanged(ref _activity, value);
    }

    #region report
    private List<ApplicationObjectViewModel> _report;
    public List<ApplicationObjectViewModel> Report
    {
      get => _report;
      private set
      {
        this.RaiseAndSetIfChanged(ref _report, value);
        this.RaisePropertyChanged("FilteredReport");
        this.RaisePropertyChanged("HasReportItems");
        this.RaisePropertyChanged("ReportFilterItems");
        this.RaisePropertyChanged("Log");
      }
    }
    public List<ApplicationObjectViewModel> FilteredReport
    {
      get
      {
        if (SearchQuery == "" && !_reportSelectedFilterItems.Any())
          return Report;
        else
        {
          var filterItems = _reportSelectedFilterItems.Any() ? Report.Where(o => _reportSelectedFilterItems.Any(a => o.Status == a)).ToList() : Report;
          return SearchQuery == "" ?
            filterItems :
            filterItems.Where(o => _searchQueryItems.All(a => o.SearchText.ToLower().Contains(a.ToLower()))).ToList();
        }
      }
    }
    public bool HasReportItems
    {
      get { return (Progress.Report.ReportObjects == null || Progress.Report.ReportObjects.Count == 0) ? false : true; }
    }
    public string Log
    {
      get
      {
        string defaultMessage = string.IsNullOrEmpty(Progress.Report.ConversionLogString) ?
          "\nWelcome to the report! \n\nObjects you send or receive will appear here to help you understand how your document has changed." :
          Progress.Report.ConversionLogString;

        string reportInfo = $"\nOperation: {(PreviewOn ? "Preview " : "")}{(IsReceiver ? "Received at " : "Sent at ")}{DateTime.Now.ToLocalTime().ToString("dd/MM/yy HH:mm:ss")}";
        reportInfo += $"\nTotal: {Report.Count} objects";
        reportInfo += Progress.Report.OperationErrors.Any() ? $"\n\nErrors: \n{Progress.Report.OperationErrorsString}" : "";

        return Report.Any() || Progress.Report.OperationErrors.Any() ? reportInfo : defaultMessage;
      }
    }

    private List<string> _searchQueryItems = new List<string>();
    private string _searchQuery = "";
    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        if (string.IsNullOrEmpty(SearchQuery))
          _searchQueryItems.Clear();
        else if (!SearchQuery.Replace(" ", "").Any())
          ClearSearchCommand();
        else
          _searchQueryItems = _searchQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        this.RaisePropertyChanged("FilteredReport");
      }
    }

    #region REPORT FILTER
    public SelectionModel<string> ReportSelectionModel { get; set; }
    private List<string> _reportSelectedFilterItems = new List<string>();
    private List<string> _reportFilterItems = new List<string>();
    public List<string> ReportFilterItems
    {
      get => _reportFilterItems;
      set
      {
        this.RaiseAndSetIfChanged(ref _reportFilterItems, value);
      }
    }
    void ReportFilterSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
      try
      {
        foreach (var a in e.SelectedItems)
          if (!_reportSelectedFilterItems.Contains(a as string))
            _reportSelectedFilterItems.Add(a as string);
        foreach (var r in e.DeselectedItems)
          if (_reportSelectedFilterItems.Contains(r as string))
            _reportSelectedFilterItems.Remove(r as string);

        this.RaisePropertyChanged("FilteredReport");
      }
      catch (Exception ex)
      {

      }
    }
    #endregion

    #endregion

    private List<CommentViewModel> _comments;
    public List<CommentViewModel> Comments
    {
      get => _comments;
      private set => this.RaiseAndSetIfChanged(ref _comments, value);
    }

    private FilterViewModel _selectedFilter;
    public FilterViewModel SelectedFilter
    {
      get => _selectedFilter;
      set
      {
        //trigger change when any property in the child model view changes
        //used for the CanSave etc button bindings
        if (value != null)
          value.PropertyChanged += (s, eo) =>
          {
            this.RaisePropertyChanged("SelectedFilter");
          };
        this.RaiseAndSetIfChanged(ref _selectedFilter, value);
      }
    }

    private List<FilterViewModel> _availableFilters;
    public List<FilterViewModel> AvailableFilters
    {
      get => _availableFilters;
      private set => this.RaiseAndSetIfChanged(ref _availableFilters, value);
    }

    private List<ReceiveMode> _receiveModes;
    public List<ReceiveMode> ReceiveModes
    {
      get => _receiveModes;
      private set => this.RaiseAndSetIfChanged(ref _receiveModes, value);
    }

    private List<ISetting> _settings;
    public List<ISetting> Settings
    {
      get => _settings;
      internal set
      {
        this.RaiseAndSetIfChanged(ref _settings, value);
        this.RaisePropertyChanged("HasSettings");
      }
    }
    public bool HasSettings => true; //AvailableSettings != null && AvailableSettings.Any();

    public string _previewImageUrl = "";
    public string PreviewImageUrl
    {
      get => _previewImageUrl;
      set
      {
        this.RaiseAndSetIfChanged(ref _previewImageUrl, value);
        DownloadImage(PreviewImageUrl);
      }
    }

    private Avalonia.Media.Imaging.Bitmap _previewImage = null;
    public Avalonia.Media.Imaging.Bitmap PreviewImage
    {
      get => _previewImage;
      set => this.RaiseAndSetIfChanged(ref _previewImage, value);
    }

    public string _previewImageUrl360 = "";
    public string PreviewImageUrl360
    {
      get => _previewImageUrl360;
      set
      {
        this.RaiseAndSetIfChanged(ref _previewImageUrl360, value);
        DownloadImage360(PreviewImageUrl360);
      }
    }

    private Avalonia.Media.Imaging.Bitmap _previewImage360 = null;
    public Avalonia.Media.Imaging.Bitmap PreviewImage360
    {
      get => _previewImage360;
      set => this.RaiseAndSetIfChanged(ref _previewImage360, value);
    }

    public bool CanOpenCommentsIn3DView { get; set; } = false;
    private bool _isAddingBranches = false;

    #endregion

    private string Url
    {
      get
      {
        //sender
        if (!IsReceiver)
        {
          if (SelectedBranch != null && SelectedBranch.Branch.name != "main")
            return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{Uri.EscapeDataString(SelectedBranch.Branch.name)}";
        }
        //receiver
        else
        {
          if (SelectedCommit != null && SelectedCommit.id != "latest")
            return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/commits/{SelectedCommit.id}";
          if (SelectedBranch != null)
            return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{Uri.EscapeDataString(SelectedBranch.Branch.name)}";
        }
        return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}";

      }
    }

    public void UpdateVisualParentAndInit(IScreen hostScreen)
    {
      HostScreen = hostScreen;
      //refresh stream, branches, filters etc
      Init();
    }
    /// <summary>
    /// Unique identifier to identify this stream view model
    /// </summary>
    private string _guid { get; set; }
    public StreamViewModel(StreamState streamState, IScreen hostScreen, ICommand removeSavedStreamCommand)
    {
      try
      {
        _guid = Guid.NewGuid().ToString();
        StreamState = streamState;
        //use cached stream, then load a fresh one async 
        //this way we can immediately show stream name and other info and update it later if it changed
        Stream = streamState.CachedStream;
        Client = streamState.Client;
        IsReceiver = streamState.IsReceiver;
        AutoReceive = streamState.AutoReceive;
        SelectedReceiveMode = streamState.ReceiveMode;

        //default to receive mode if no permission to send
        if (Stream.role == null || Stream.role == "stream:reviewer")
        {
          IsReceiver = true;
        }

        HostScreen = hostScreen;
        RemoveSavedStreamCommand = removeSavedStreamCommand;
        Collaborators = new CollaboratorsViewModel(HostScreen, this);

        //use dependency injection to get bindings
        Bindings = Locator.Current.GetService<ConnectorBindings>();
        CanOpenCommentsIn3DView = Bindings.CanOpen3DView;

        if (Client == null)
        {
          NoAccess = true;
          return;
        }

        Init();
        Subscribe();
        GenerateMenuItems();

        var updateTextTimer = new System.Timers.Timer();
        updateTextTimer.Elapsed += UpdateTextTimer_Elapsed;
        updateTextTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
        updateTextTimer.Enabled = true;
      }
      catch (Exception ex)
      {
        new SpeckleException("Error creating stream view model", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private void Init()
    {
      try
      {
        GetStream().ConfigureAwait(false);
        GetBranchesAndRestoreState();
        GetActivity();
        GetReport();
        GetComments();

      }
      catch (Exception ex)
      {
        new SpeckleException("Error creating stream view model", ex, true, Sentry.SentryLevel.Error);
      }
    }



    private void UpdateTextTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      this.RaisePropertyChanged("LastUsed");
      this.RaisePropertyChanged("LastUpdated");
    }

    private void GenerateMenuItems()
    {
      try
      {
        var menu = new MenuItemViewModel { Header = new MaterialIcon { Kind = MaterialIconKind.EllipsisVertical, Foreground = Avalonia.Media.Brushes.White } };
        menu.Items = new List<MenuItemViewModel> {
        new MenuItemViewModel (ViewOnlineSavedStreamCommand, "View online",  MaterialIconKind.ExternalLink),
        new MenuItemViewModel (CopyStreamURLCommand, "Copy URL to clipboard",  MaterialIconKind.ContentCopy),
      };
        var customMenues = Bindings.GetCustomStreamMenuItems();
        if (customMenues != null)
          menu.Items.AddRange(customMenues.Select(x => new MenuItemViewModel(x, StreamState)).ToList());
        //remove is added last
        //menu.Items.Add(new MenuItemViewModel(RemoveSavedStreamCommand, StreamState.Id, "Remove", MaterialIconKind.Bin));
        MenuItems.Add(menu);

        this.RaisePropertyChanged("MenuItems");
      }
      catch (Exception ex)
      {
        new SpeckleException("Error generating menu items", ex, true, Sentry.SentryLevel.Error);
      }
    }

    public async Task GetStream()
    {
      try
      {
        Stream = await Client.StreamGet(StreamState.StreamId, 25);
        if (Stream.role == "stream:owner")
        {
          var streamPendingCollaborators = await Client.StreamGetPendingCollaborators(StreamState.StreamId);
          Stream.pendingCollaborators = streamPendingCollaborators.pendingCollaborators;
        }
        Collaborators.ReloadUsers(); ;

        StreamState.CachedStream = Stream;
      }
      catch (Exception e)
      {
        new SpeckleException("Error retrieving stream", e, true, Sentry.SentryLevel.Error);
      }
    }

    internal async void GetBranchesAndRestoreState()
    {
      try
      {
        //receive modes
        ReceiveModes = Bindings.GetReceiveModes();
        //by default the first available receive mode is selected
        SelectedReceiveMode = ReceiveModes.Contains(StreamState.ReceiveMode) ? StreamState.ReceiveMode : ReceiveModes[0];

        //get available settings from our bindings
        Settings = Bindings.GetSettings();

        //get available filters from our bindings
        AvailableFilters = new List<FilterViewModel>(Bindings.GetSelectionFilters().Select(x => new FilterViewModel(x)));
        SelectedFilter = AvailableFilters[0];

        Branches = await Client.StreamGetBranches(Stream.id, 100, 0);

        var index = Branches.FindIndex(x => x.name == StreamState.BranchName);
        if (index != -1)
          SelectedBranch = BranchesViewModel[index];
        else
          SelectedBranch = BranchesViewModel[0];

        //restore selected filter
        if (StreamState.Filter != null)
        {
          SelectedFilter = AvailableFilters.FirstOrDefault(x => x.Filter.Slug == StreamState.Filter.Slug);
          if (SelectedFilter != null)
            SelectedFilter.Filter = StreamState.Filter;
        }
        else
        {
          var selectionFilter = AvailableFilters.FirstOrDefault(x => x.Filter.Type == typeof(ManualSelectionFilter).ToString());
          //if there are any selected objects, set the manual selection automagically
          if (selectionFilter != null && Bindings.GetSelectedObjects().Any())
          {
            SelectedFilter = selectionFilter;
            SelectedFilter.AddObjectSelection();
          }
        }
        if (StreamState.Settings != null)
        {
          foreach (var setting in Settings)
          {
            var savedSetting = StreamState.Settings.FirstOrDefault(o => o.Slug == setting.Slug);
            if (savedSetting != null)
              setting.Selection = savedSetting.Selection;
          }
        }
      }
      catch (Exception ex)
      {
        new SpeckleException("Error restoring stream state", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private void GetReport()
    {
      var report = new List<ApplicationObjectViewModel>();
      foreach (var applicationObject in Progress.Report.ReportObjects)
      {
        var rvm = new ApplicationObjectViewModel(applicationObject, StreamState.IsReceiver, Progress.Report);
        report.Add(rvm);
      }
      Report = report;

      if (HasReportItems) // activate report tab
      {
        SelectedTab = 4;
      }

      // report filter selection
      ReportSelectionModel = new SelectionModel<string>();
      ReportSelectionModel.SingleSelect = false;
      ReportSelectionModel.SelectionChanged += ReportFilterSelectionChanged;
      ReportFilterItems = report.Select(o => o.Status).Distinct().ToList();
    }

    private async void GetActivity()
    {
      try
      {
        var filteredActivity = (await Client.StreamGetActivity(Stream.id))
          .Where(x => x.actionType == "commit_create" || x.actionType == "commit_receive" || x.actionType == "stream_create")
          .Reverse().ToList();
        var activity = new List<ActivityViewModel>();
        foreach (var a in filteredActivity)
        {
          var avm = new ActivityViewModel(a, Client);
          activity.Add(avm);
        }
        Activity = activity;
        ScrollToBottom();
      }
      catch (Exception ex)
      {
        new SpeckleException("Error getting activity", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private async Task GetComments()
    {
      try
      {
        var commentData = await Client.StreamGetComments(Stream.id);
        var comments = new List<CommentViewModel>();
        foreach (var c in commentData.items)
        {
          var cvm = new CommentViewModel(c, Stream.id, Client);
          comments.Add(cvm);
        }
        Comments = comments;
      }
      catch (Exception ex)
      {
        new SpeckleException("Error getting comments", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private async void ScrollToBottom()
    {
      try
      {
        if (StreamEditView.Instance != null)
        {
          await Task.Delay(250);
          Avalonia.Threading.Dispatcher.UIThread.Post(() =>
          {
            var scroller = StreamEditView.Instance.FindControl<ScrollViewer>("activityScroller");
            if (scroller != null)
              scroller.ScrollToEnd();
          });
        }
      }
      catch (Exception ex)
      {

      }
    }

    /// <summary>
    /// Update the model Stream state whenever we send, receive or save a stream
    /// </summary>
    private void UpdateStreamState()
    {
      try
      {
        StreamState.BranchName = SelectedBranch.Branch.name;
        StreamState.IsReceiver = IsReceiver;
        StreamState.AutoReceive = AutoReceive;
        StreamState.ReceiveMode = SelectedReceiveMode;

        if (IsReceiver)
          StreamState.CommitId = SelectedCommit.id;
        if (!IsReceiver)
          StreamState.Filter = SelectedFilter.Filter;
        StreamState.Settings = Settings.Select(o => o).ToList();
      }
      catch (Exception ex)
      {
        new SpeckleException("Error updating state", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private async Task GetBranches()
    {
      var prevBranchName = SelectedBranch != null ? SelectedBranch.Branch.name : StreamState.BranchName;
      Branches = await Client.StreamGetBranches(Stream.id, 100, 0);

      var index = Branches.FindIndex(x => x.name == prevBranchName);
      if (index != -1)
        SelectedBranch = BranchesViewModel[index];
      else
        SelectedBranch = BranchesViewModel[0];

    }
    private async Task GetCommits()
    {
      try
      {
        var prevCommitId = SelectedCommit != null ? SelectedCommit.id : StreamState.CommitId;
        var branch = await Client.BranchGet(Stream.id, SelectedBranch.Branch.name, 100);
        if (branch != null && branch.commits.items.Any())
        {
          branch.commits.items.Insert(0, new Commit { id = "latest", message = "Always receive the latest commit sent to this branch." });
          Commits = branch.commits.items;

          var commit = Commits.FirstOrDefault(x => x.id == prevCommitId);
          if (commit != null)
            SelectedCommit = commit;
          else
            SelectedCommit = Commits[0];
        }

        else
        {
          SelectedCommit = null;
          Commits = new List<Commit>();
        }
      }
      catch (Exception ex)
      {
        new SpeckleException("Error getting commits", ex, true, Sentry.SentryLevel.Error);
      }
    }


    #region subscriptions
    private void Subscribe()
    {
      Client.SubscribeCommitCreated(StreamState.StreamId);
      Client.SubscribeCommitUpdated(StreamState.StreamId);
      Client.SubscribeCommitDeleted(StreamState.StreamId);
      Client.OnCommitCreated += Client_OnCommitCreated;
      Client.OnCommitUpdated += Client_OnCommitChange;
      Client.OnCommitDeleted += Client_OnCommitChange;

      Client.SubscribeBranchCreated(StreamState.StreamId);
      Client.SubscribeBranchUpdated(StreamState.StreamId);
      Client.SubscribeBranchDeleted(StreamState.StreamId);
      Client.OnBranchCreated += Client_OnBranchChange;
      Client.OnBranchUpdated += Client_OnBranchChange;
      Client.OnBranchDeleted += Client_OnBranchChange;

      Client.SubscribeCommentActivity(StreamState.StreamId);
      Client.OnCommentActivity += Client_OnCommentActivity;

      Client.SubscribeStreamUpdated(StreamState.StreamId);
      Client.OnStreamUpdated += Client_OnStreamUpdated;
    }

    private async void Client_OnCommentActivity(object sender, CommentItem e)
    {
      await GetComments();

      var authorName = "you";
      if (e.authorId != Client.Account.userInfo.id)
      {
        var author = await Client.OtherUserGet(e.id);
        authorName = author.name;
      }

      bool openStream = true;
      var svm = MainViewModel.RouterInstance.NavigationStack.Last() as StreamViewModel;
      if (svm != null && svm.Stream.id == Stream.id)
        openStream = false;

      Avalonia.Threading.Dispatcher.UIThread.Post(() =>
      {
        MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
        {
          Title = $"🆕 New comment by {authorName}:",
          Message = e.rawText,
          OnClick = () =>
          {
            if (openStream)
              MainViewModel.RouterInstance.Navigate.Execute(this);

            SelectedTab = 3;
          }
          ,
          Type = Avalonia.Controls.Notifications.NotificationType.Success,
          Expiration = TimeSpan.FromSeconds(15)
        });
      });
    }

    private async void Client_OnBranchChange(object sender, Speckle.Core.Api.SubscriptionModels.BranchInfo info)
    {
      if (!_isAddingBranches)
        await GetBranches();
    }


    private async void Client_OnCommitChange(object sender, Speckle.Core.Api.SubscriptionModels.CommitInfo info)
    {
      if (info.branchName == SelectedBranch.Branch.name)
        await GetCommits();
    }

    private async void Client_OnCommitCreated(object sender, Speckle.Core.Api.SubscriptionModels.CommitInfo info)
    {
      try
      {


        if (info.branchName == SelectedBranch.Branch.name)
          await GetCommits();

        if (!IsReceiver) return;

        var authorName = "You";
        if (info.authorId != Client.Account.userInfo.id)
        {
          var author = await Client.OtherUserGet(info.id);
          authorName = author.name;
        }

        bool openOnline = false;

        //if in stream edit open online
        var svm = MainViewModel.RouterInstance.NavigationStack.Last() as StreamViewModel;
        if (svm != null && svm.Stream.id == Stream.id)
          openOnline = true;


        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
          MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
          {
            Title = $"🆕 {authorName} sent to {Stream.name}/{info.branchName}'",
            Message = openOnline ? "Click to view it online" : "Click open the stream",
            OnClick = () =>
            {
              //if in stream edit open online
              if (openOnline)
                ViewOnlineSavedStreamCommand();
              //if on home, open stream
              else
                MainViewModel.RouterInstance.Navigate.Execute(this);

            }
            ,
            Type = Avalonia.Controls.Notifications.NotificationType.Success,
            Expiration = TimeSpan.FromSeconds(10)
          });
        });

        ScrollToBottom();

        if (AutoReceive)
          ReceiveCommand();
      }
      catch (Exception ex)
      {

      }
    }

    private void Client_OnStreamUpdated(object sender, Speckle.Core.Api.SubscriptionModels.StreamInfo e)
    {
      GetStream().ConfigureAwait(false);
    }

    #endregion

    public async Task DownloadImage(string url)
    {
      try
      {

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Client.ApiToken}");
        var result = await httpClient.GetAsync(url);


        byte[] bytes = await result.Content.ReadAsByteArrayAsync();

        System.IO.Stream stream = new MemoryStream(bytes);

        _previewImage = new Bitmap(stream);
        this.RaisePropertyChanged(nameof(PreviewImage));

      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(ex);
        _previewImage = null; // Could not download...
      }
    }

    //could not find a simple way to use a single method
    public async Task DownloadImage360(string url)
    {
      try
      {

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Client.ApiToken}");
        var result = await httpClient.GetAsync(url);


        byte[] bytes = await result.Content.ReadAsByteArrayAsync();

        System.IO.Stream stream = new MemoryStream(bytes);

        _previewImage360 = new Bitmap(stream);
        this.RaisePropertyChanged(nameof(PreviewImage360));

      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(ex);
        _previewImage360 = null; // Could not download...
      }
    }


    #region commands

    private async void AddNewBranch()
    {
      var dialog = new NewBranchDialog();
      var nbvm = new NewBranchViewModel(Branches);
      dialog.DataContext = nbvm;

      var result = await dialog.ShowDialog<bool>();

      if (result)
      {
        try
        {
          _isAddingBranches = true;
          var branchId = await StreamState.Client.BranchCreate(new BranchCreateInput { streamId = Stream.id, description = nbvm.Description ?? "", name = nbvm.BranchName });
          await GetBranches();

          var index = Branches.FindIndex(x => x.name == nbvm.BranchName);
          if (index != -1)
            SelectedBranch = BranchesViewModel[index];

          Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Branch Create" } });

        }
        catch (Exception e)
        {
          Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
          new SpeckleException("Error creating branch", e, true, Sentry.SentryLevel.Error);
        }
        finally
        {
          _isAddingBranches = false;
        }

      }
      else
      {
        //make sure the a branch is selected if canceled
        SelectedBranch = BranchesViewModel[0];
      }
    }
    public async void CopyReportCommand()
    {
      var reportObjectSummaries = FilteredReport.Select(o => o.GetSummary()).ToArray();
      var summary = string.Join("\n", reportObjectSummaries);

      await Avalonia.Application.Current.Clipboard.SetTextAsync(summary);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Copy Report" } });
    }
    public void ClearSearchCommand()
    {
      SearchQuery = "";
    }


    public void ShareCommand()
    {
      MainViewModel.RouterInstance.Navigate.Execute(new CollaboratorsViewModel(HostScreen, this));

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Share Open" } });
    }

    public void EditSavedStreamCommand()
    {
      MainViewModel.RouterInstance.Navigate.Execute(this);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Edit" } });
    }

    public async void ViewOnlineSavedStreamCommand()
    {
      //ensure click transition has finished
      await Task.Delay(100);

      OpenUrl(Url);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream View" } });
    }

    private void OpenUrl(string url)
    {
      //to open urls in .net core must set UseShellExecute = true
      Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public async void CopyStreamURLCommand()
    {
      //ensure click transition has finished
      await Task.Delay(100);
      Avalonia.Application.Current.Clipboard.SetTextAsync(Url);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Copy Link" } });
    }

    public async void SendCommand()
    {
      try
      {
        UpdateStreamState();

        HomeViewModel.Instance.AddSavedStream(this); //save the stream as well

        Reset();

        if (!await Helpers.UserHasInternet())
        {
          Dispatcher.UIThread.Post(() =>
            MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
            {
              Title = "⚠️ Oh no!",
              Message = "Could not reach the internet, are you connected?",
              Type = Avalonia.Controls.Notifications.NotificationType.Error
            }), DispatcherPriority.Background);

          return;
        }

        Progress.IsProgressing = true;
        var commitId = await Task.Run(() => Bindings.SendStream(StreamState, Progress));
        Progress.IsProgressing = false;

        if (!Progress.CancellationTokenSource.IsCancellationRequested && commitId != null)
        {
          LastUsed = DateTime.Now.ToString();
          var view = MainViewModel.RouterInstance.NavigationStack.Last() is StreamViewModel ? "Stream" : "Home";

          Analytics.TrackEvent(Client.Account, Analytics.Events.Send, new Dictionary<string, object> {
            { "filter", StreamState.Filter.Name },
            { "view", view },
            { "collaborators", Stream.collaborators.Count },
            { "isMain", SelectedBranch.Branch.name == "main" ? true : false },
            { "branches", Stream.branches?.totalCount },
            { "commits", Stream.commits?.totalCount },
            { "savedStreams", HomeViewModel.Instance.SavedStreams?.Count },
          });

          MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
          {
            Title = "👌 Data sent",
            Message = $"Sent to '{Stream.name}', view it online",
            OnClick = () => OpenUrl($"{StreamState.ServerUrl}/streams/{StreamState.StreamId}/commits/{commitId}"),
            Type = Avalonia.Controls.Notifications.NotificationType.Success,
            Expiration = TimeSpan.FromSeconds(10)
          });
        }
        else
        {
          MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
          {
            Title = "😖 Send Error",
            Message = $"Something went wrong",
            Type = Avalonia.Controls.Notifications.NotificationType.Error
          });
        }

        GetActivity();
        GetReport();
      }
      catch (Exception ex)
      {
        new SpeckleException("Error sending", ex, true, Sentry.SentryLevel.Error);
      }
    }

    public async void PreviewCommand()
    {
      PreviewOn = !PreviewOn;
      if (PreviewOn)
      {
        try
        {
          UpdateStreamState();

          Progress.CancellationTokenSource = new System.Threading.CancellationTokenSource();
          Progress.IsPreviewProgressing = true;
          if (IsReceiver)
          {
            Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Preview Receive" } });
            await Task.Run(() => Bindings.PreviewReceive(StreamState, Progress));
          }
          if (!IsReceiver)
          {
            Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Preview Send" } });
            await Task.Run(() => Bindings.PreviewSend(StreamState, Progress));
          }
          Progress.IsPreviewProgressing = false;
          GetReport();
        }
        catch (Exception ex)
        {
          new SpeckleException("Error preview", ex, true, Sentry.SentryLevel.Error);
        }
      }
      else
      {
        Progress.CancellationTokenSource.Cancel();
        Bindings.ResetDocument();
      }
    }

    public async void ReceiveCommand()
    {
      try
      {
        UpdateStreamState();
        //save the stream as well
        HomeViewModel.Instance.AddSavedStream(this);

        Reset();

        if (!await Helpers.UserHasInternet())
        {
          Dispatcher.UIThread.Post(() =>
            MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
            {
              Title = "⚠️ Oh no!",
              Message = "Could not reach the internet, are you connected?",
              Type = Avalonia.Controls.Notifications.NotificationType.Error
            }), DispatcherPriority.Background);

          return;
        }


        Progress.IsProgressing = true;
        var state = await Task.Run(() => Bindings.ReceiveStream(StreamState, Progress));
        Progress.IsProgressing = false;
        var view = MainViewModel.RouterInstance.NavigationStack.Last() is StreamViewModel ? "Stream" : "Home";

        if (!Progress.CancellationTokenSource.IsCancellationRequested)
        {
          LastUsed = DateTime.Now.ToString();
          Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.Receive,
            new Dictionary<string, object>() {
              { "mode", StreamState.ReceiveMode },
              { "auto", StreamState.AutoReceive },
              { "sourceHostApp", HostApplications.GetHostAppFromString(state.LastSourceApp).Slug },
              { "sourceHostAppVersion", state.LastSourceApp },
              { "view", view },
              { "collaborators", Stream.collaborators.Count },
              { "isMain", SelectedBranch.Branch.name == "main" ? true : false },
              { "branches", Stream.branches?.totalCount },
              { "commits", Stream.commits?.totalCount },
              { "savedStreams", HomeViewModel.Instance.SavedStreams?.Count }

            });
        }


        GetActivity();
        GetReport();
      }
      catch (Exception ex)
      {
        new SpeckleException("Error receiving", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private void Reset()
    {
      Progress = new ProgressViewModel();
    }

    public void CancelSendOrReceiveCommand()
    {
      Progress.CancellationTokenSource.Cancel();
      Reset();
      string cancelledEvent = IsReceiver ? "Cancel Receive" : "Cancel Send";
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", cancelledEvent } });

      MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
      {
        Title = "❌ Operation cancelled",
        Message = IsReceiver ? "Nothing was received" : "Nothing was sent",
        Type = Avalonia.Controls.Notifications.NotificationType.Success
      });
    }

    public void CancelPreviewCommand()
    {
      Progress.CancellationTokenSource.Cancel();
      string cancelledEvent = IsReceiver ? "Cancel Preview Receive" : "Cancel Preview Send";
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", cancelledEvent } });
      Progress.IsPreviewProgressing = false;
      PreviewOn = false;
    }

    private void SaveCommand()
    {
      try
      {
        UpdateStreamState();
        MainViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
        HomeViewModel.Instance.AddSavedStream(this);

        if (IsReceiver)
          Analytics.TrackEvent(Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Receiver Add" } });
        else
          Analytics.TrackEvent(Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Sender Add" } });

        MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
        {
          Title = "💾 Stream Saved",
          Message = "This stream has been saved to this file",
          Type = Avalonia.Controls.Notifications.NotificationType.Success
        });
      }
      catch (Exception ex)
      {
        new SpeckleException("Error saving", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private void OpenSettingsCommand()
    {
      try
      {
        var settingsPageViewModel = new SettingsPageViewModel(HostScreen, Settings.Select(x => new SettingViewModel(x)).ToList(), this);
        MainViewModel.RouterInstance.Navigate.Execute(settingsPageViewModel);
        Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Settings Open" } });
      }
      catch (Exception e)
      {
      }
    }

    private void AskRemoveSavedStreamCommand()
    {
      IsRemovingStream = true;
    }

    private void CancelRemoveSavedStreamCommand()
    {
      IsRemovingStream = false;
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedFilter))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSaveCommand(object parameter)
    {
      return true;
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedFilter))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSendCommand(object parameter)
    {
      return IsReady();
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanReceiveCommand(object parameter)
    {
      return IsReady();
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(SelectedFilter))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanPreviewCommand(object parameter)
    {
      bool previewImplemented = IsReceiver ? Bindings.CanPreviewReceive : Bindings.CanPreviewSend;
      if (previewImplemented)
        return IsReady();
      else return false;
    }

    private bool IsReady()
    {
      if (NoAccess)
        return false;
      if (SelectedBranch == null)
        return false;

      if (!IsReceiver)
      {
        if (SelectedFilter == null)
          return false;
        if (!SelectedFilter.IsReady())
          return false;
      }
      else
      {
        if (SelectedCommit == null)
          return false;
      }

      return true;
    }

    public void Dispose()
    {
      Client.Dispose();
    }
    #endregion

  }
}
