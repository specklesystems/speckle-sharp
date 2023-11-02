using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Selection;
using Avalonia.Media;
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
using Material.Dialog.Icons;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Serilog.Events;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Helpers;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Splat;
using Stream = Speckle.Core.Api.Stream;

namespace DesktopUI2.ViewModels;

public class StreamViewModel : ReactiveObject, IRoutableViewModel, IDisposable
{
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
        IsReceiver = true;

      HostScreen = hostScreen;
      RemoveSavedStreamCommand = removeSavedStreamCommand;
      Collaborators = new CollaboratorsViewModel(HostScreen, this);

      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      CanOpenCommentsIn3DView = Bindings.CanOpen3DView;
      CanReceive = Bindings.CanReceive;

      if (Client == null)
      {
        NoAccess = true;
        return;
      }

      Init();
      Subscribe();
      GenerateMenuItems();

      var updateTextTimer = new Timer();
      updateTextTimer.Elapsed += UpdateTextTimer_Elapsed;
      updateTextTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
      updateTextTimer.Enabled = true;
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.ForContext(nameof(StreamState), streamState).Fatal(ex, "Failed to create stream view model");
    }
  }

  public StreamState StreamState { get; set; }

  private string Url
  {
    get
    {
      var config = ConfigManager.Load();
      if (config.UseFe2)
      {
        //sender
        if (!IsReceiver)
        {
          if (SelectedBranch != null && SelectedBranch.Branch.name != "main")
            return $"{StreamState.ServerUrl.TrimEnd('/')}/projects/{StreamState.StreamId}/models/{SelectedBranch.Branch.id}";
        }
        //receiver
        else
        {
          if (SelectedCommit != null && SelectedCommit.id != ConnectorHelpers.LatestCommitString)
            return $"{StreamState.ServerUrl.TrimEnd('/')}/projects/{StreamState.StreamId}/models/{SelectedBranch.Branch.id}@{SelectedCommit.id}";
          if (SelectedBranch != null)
            return $"{StreamState.ServerUrl.TrimEnd('/')}/projects/{StreamState.StreamId}/models/{SelectedBranch.Branch.id}";
        }

        return $"{StreamState.ServerUrl.TrimEnd('/')}/projects/{StreamState.StreamId}";
      }

      //sender
      if (!IsReceiver)
      {
        if (SelectedBranch != null && SelectedBranch.Branch.name != "main")
          return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{Uri.EscapeDataString(SelectedBranch.Branch.name)}";
      }
      //receiver
      else
      {
        if (SelectedCommit != null && SelectedCommit.id != ConnectorHelpers.LatestCommitString)
          return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/commits/{SelectedCommit.id}";
        if (SelectedBranch != null)
          return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{Uri.EscapeDataString(SelectedBranch.Branch.name)}";
      }

      return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}";
    }
  }

  /// <summary>
  /// Unique identifier to identify this stream view model
  /// </summary>
  private string _guid { get; set; }

  public IScreen HostScreen { get; set; }

  public void UpdateVisualParentAndInit(IScreen hostScreen)
  {
    HostScreen = hostScreen;
    //refresh stream, branches, filters etc
    Init();
  }

  private void Init()
  {
    try
    {
      GetStream().ConfigureAwait(true);
      GetBranchesAndRestoreState();
      GetActivity();
      GetReport();
      GetComments();
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(ex, "Failed to initialise stream view model");
      throw;
    }
  }

  private void UpdateTextTimer_Elapsed(object sender, ElapsedEventArgs e)
  {
    this.RaisePropertyChanged(nameof(LastUsed));
    this.RaisePropertyChanged(nameof(LastUpdated));
  }

  private void GenerateMenuItems()
  {
    try
    {
      var menu = new MenuItemViewModel
      {
        Header = new MaterialIcon { Kind = MaterialIconKind.EllipsisVertical, Foreground = Brushes.White }
      };
      menu.Items = new List<MenuItemViewModel>
      {
        new(ViewOnlineSavedStreamCommand, "View online", MaterialIconKind.ExternalLink),
        new(CopyStreamURLCommand, "Copy URL to clipboard", MaterialIconKind.ContentCopy)
      };
      var customMenues = Bindings.GetCustomStreamMenuItems();
      if (customMenues != null)
        menu.Items.AddRange(customMenues.Select(x => new MenuItemViewModel(x, StreamState)).ToList());
      //remove is added last
      //menu.Items.Add(new MenuItemViewModel(RemoveSavedStreamCommand, StreamState.Id, "Remove", MaterialIconKind.Bin));
      MenuItems.Add(menu);

      this.RaisePropertyChanged(nameof(MenuItems));
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed to generate menu items {exceptionMessage}", ex.Message);
    }
  }

  public async Task GetStream()
  {
    try
    {
      Stream = await Client.StreamGet(StreamState.StreamId, 25).ConfigureAwait(true);
      if (Stream.role == "stream:owner")
      {
        var streamPendingCollaborators = await Client
          .StreamGetPendingCollaborators(StreamState.StreamId)
          .ConfigureAwait(true);
        Stream.pendingCollaborators = streamPendingCollaborators.pendingCollaborators;
      }

      Collaborators.ReloadUsers();
      ;

      StreamState.CachedStream = Stream;
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed retrieving stream");
    }
  }

  internal async void GetBranchesAndRestoreState()
  {
    try
    {
      if (CanReceive)
      {
        //receive modes
        ReceiveModes = Bindings.GetReceiveModes();

        if (!ReceiveModes.Any())
          throw new InvalidOperationException("No Receive Mode is available.");

        //by default the first available receive mode is selected
        SelectedReceiveMode = ReceiveModes.Contains(StreamState.ReceiveMode)
          ? StreamState.ReceiveMode
          : ReceiveModes[0];
      }

      //get available settings from our bindings
      Settings = Bindings.GetSettings();
      HasSettings = Settings.Any();

      //get available filters from our bindings
      AvailableFilters = new List<FilterViewModel>(Bindings.GetSelectionFilters().Select(x => new FilterViewModel(x)));
      SelectedFilter = AvailableFilters[0];

      Branches = await Client.StreamGetBranches(Stream.id, 100, 0).ConfigureAwait(true);

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
        var selectionFilter = AvailableFilters.FirstOrDefault(
          x => x.Filter.Type == typeof(ManualSelectionFilter).ToString()
        );
        //if there are any selected objects, set the manual selection automagically
        if (selectionFilter != null && Bindings.GetSelectedObjects().Any())
        {
          SelectedFilter = selectionFilter;
          SelectedFilter.AddObjectSelection();
        }
      }

      if (StreamState.Settings != null)
        foreach (var setting in Settings)
        {
          var savedSetting = StreamState.Settings.FirstOrDefault(o => o.Slug == setting.Slug);
          if (savedSetting != null)
            setting.Selection = savedSetting.Selection;
        }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed restoring stream state {exceptionMessage}", ex.Message);
    }
  }

  private void GetReport()
  {
    var report = new List<ApplicationObjectViewModel>();
    foreach (var applicationObject in Progress.Report.ReportObjects.Values)
    {
      var rvm = new ApplicationObjectViewModel(applicationObject, StreamState.IsReceiver, Progress.Report);
      report.Add(rvm);
    }

    Report = report;

    //do not switch to report tab automatically
    //if (HasReportItems)
    //{
    //  // activate report tab
    //  SelectedTab = 4;
    //}

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
      var filteredActivity = (await Client.StreamGetActivity(Stream.id).ConfigureAwait(true))
        .Where(
          x => x.actionType == "commit_create" || x.actionType == "commit_receive" || x.actionType == "stream_create"
        )
        .Reverse()
        .ToList();
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
      SpeckleLog.Logger.Error(ex, "Failed getting activity {exceptionMessage}", ex.Message);
    }
  }

  private async Task GetComments()
  {
    try
    {
      var commentData = await Client.StreamGetComments(Stream.id).ConfigureAwait(true);
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
      SpeckleLog.Logger.Error(ex, "Failed getting comments {exceptionMessage}", ex.Message);
    }
  }

  private async void ScrollToBottom()
  {
    try
    {
      if (StreamEditView.Instance != null)
      {
        await Task.Delay(250).ConfigureAwait(true);
        Dispatcher.UIThread.Post(() =>
        {
          var scroller = StreamEditView.Instance.FindControl<ScrollViewer>("activityScroller");
          if (scroller != null)
            scroller.ScrollToEnd();
        });
      }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(
        ex,
        "Swallowing exception in {methodName}: {exceptionMessage}",
        nameof(ScrollToBottom),
        ex.Message
      );
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
      SpeckleLog.Logger.Error(ex, "Failed updating stream state {exceptionMessage}", ex.Message);
    }
  }

  private async Task GetBranches()
  {
    try
    {
      var prevBranchName = SelectedBranch != null ? SelectedBranch.Branch.name : StreamState.BranchName;
      Branches = await Client.StreamGetBranches(Stream.id, 500, 0).ConfigureAwait(true);

      var index = Branches.FindIndex(x => x.name == prevBranchName);
      if (index != -1)
        SelectedBranch = BranchesViewModel[index];
      else
        SelectedBranch = BranchesViewModel[0];
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed updating stream state {exceptionMessage}", ex.Message);
    }
  }

  private async Task GetCommits()
  {
    try
    {
      var prevCommitId = SelectedCommit != null ? SelectedCommit.id : StreamState.CommitId;
      var branch = await Client.BranchGet(Stream.id, SelectedBranch.Branch.name, 100).ConfigureAwait(true);
      if (branch != null && branch.commits.items.Any())
      {
        branch.commits.items.Insert(
          0,
          new Commit
          {
            id = ConnectorHelpers.LatestCommitString,
            message = "Always receive the latest commit sent to this branch."
          }
        );
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
      SpeckleLog.Logger.Error(ex, "Failed getting commits {exceptionMessage}", ex.Message);
    }
  }

  public async Task DownloadImage(string url)
  {
    try
    {
      using var httpClient = new HttpClient();
      Http.AddAuthHeader(httpClient, Client.ApiToken);

      var result = await httpClient.GetAsync(url).ConfigureAwait(true);

      byte[] bytes = await result.Content.ReadAsByteArrayAsync().ConfigureAwait(true);

      System.IO.Stream stream = new MemoryStream(bytes);

      _previewImage = new Bitmap(stream);
      this.RaisePropertyChanged(nameof(PreviewImage));
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(
        ex,
        "Swallowing exception in {methodName}: {exceptionMessage}",
        nameof(DownloadImage),
        ex.Message
      );
      Debug.WriteLine(ex);
      _previewImage = null; // Could not download...
    }
  }

  //could not find a simple way to use a single method
  public async Task DownloadImage360(string url)
  {
    try
    {
      using var httpClient = new HttpClient();
      Http.AddAuthHeader(httpClient, Client.ApiToken);

      var result = await httpClient.GetAsync(url).ConfigureAwait(true);

      byte[] bytes = await result.Content.ReadAsByteArrayAsync().ConfigureAwait(true);

      System.IO.Stream stream = new MemoryStream(bytes);

      _previewImage360 = new Bitmap(stream);
      this.RaisePropertyChanged(nameof(PreviewImage360));
      //the default 360 image width is 34300
      //this is a quick hack to see if the returned image is not an error image like "you do not have access" etc
      if (_previewImage360.Size.Width > 30000)
        PreviewImage360Loaded = true;
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger
        .ForContext("imageUrl", url)
        .Warning(ex, "Swallowing exception in {methodName}: {exceptionMessage}", nameof(DownloadImage360), ex.Message);
      Debug.WriteLine(ex);
      _previewImage360 = null; // Could not download...
    }
  }

  #region bindings

  private ConnectorBindings Bindings;

  private CollaboratorsViewModel Collaborators { get; set; }

  public ICommand RemoveSavedStreamCommand { get; }

  private bool _previewOn;

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

  private ProgressViewModel _progress = new();

  public ProgressViewModel Progress
  {
    get => _progress;
    set => this.RaiseAndSetIfChanged(ref _progress, value);
  }

  private List<MenuItemViewModel> _menuItems = new();

  public List<MenuItemViewModel> MenuItems
  {
    get => _menuItems;
    private set => this.RaiseAndSetIfChanged(ref _menuItems, value);
  }

  /// Human Readable String
  public string LastUpdated => "Updated " + Formatting.TimeAgo(StreamState.CachedStream.updatedAt);

  /// Human Readable String
  public string LastUsed
  {
    get
    {
      var verb = StreamState.IsReceiver ? "Received" : "Sent";
      if (StreamState.LastUsed == null)
        return $"Never {verb.ToLower()}";
      return $"{verb} {Formatting.TimeAgo(StreamState.LastUsed)}";
    }
  }

  public DateTime? LastUsedTime
  {
    get => StreamState.LastUsed;
    set
    {
      StreamState.LastUsed = value;
      this.RaisePropertyChanged(nameof(LastUsed));
    }
  }

  public bool UseFe2
  {
    get
    {
      var config = ConfigManager.Load();
      return config.UseFe2;
    }
  }

  private bool _isRemovingStream;

  public bool IsRemovingStream
  {
    get => _isRemovingStream;
    private set
    {
      this.RaiseAndSetIfChanged(ref _isRemovingStream, value);
      this.RaisePropertyChanged(nameof(StreamEnabled));
    }
  }

  public bool StreamEnabled => !IsRemovingStream && !NoAccess;

  private bool _isExpanded;

  public bool IsExpanded
  {
    get => _isExpanded;
    private set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
  }

  private int _selectedTab;

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
  public bool NoAccess { get; set; }

  private bool _isReceiver;

  public bool IsReceiver
  {
    get => _isReceiver;
    set
    {
      if (value != _isReceiver)
        PreviewOn = false;
      this.RaiseAndSetIfChanged(ref _isReceiver, value);
      this.RaisePropertyChanged(nameof(BranchesViewModel));
    }
  }

  private bool _autoReceive;

  public bool AutoReceive
  {
    get => _autoReceive;
    set => this.RaiseAndSetIfChanged(ref _autoReceive, value);
  }

  private ReceiveMode _selectedReceiveMode;

  public ReceiveMode SelectedReceiveMode
  {
    get => _selectedReceiveMode;
    set => this.RaiseAndSetIfChanged(ref _selectedReceiveMode, value);
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
      PreviewImage360Loaded = false;
      if (_selectedCommit != null)
      {
        if (_selectedCommit.id == ConnectorHelpers.LatestCommitString)
          PreviewImageUrl =
            Client.Account.serverInfo.url
            + $"/preview/{Stream.id}/branches/{Uri.EscapeDataString(SelectedBranch.Branch.name)}";
        else
          PreviewImageUrl = Client.Account.serverInfo.url + $"/preview/{Stream.id}/commits/{_selectedCommit.id}";
        PreviewImageUrl360 = $"{PreviewImageUrl}/all";
      }
    }
  }

  private List<Commit> _commits = new();

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
      this.RaisePropertyChanged(nameof(FilteredReport));
      this.RaisePropertyChanged(nameof(HasReportItems));
      this.RaisePropertyChanged(nameof(ReportFilterItems));
      this.RaisePropertyChanged(nameof(Log));
    }
  }

  public List<ApplicationObjectViewModel> FilteredReport
  {
    get
    {
      if (string.IsNullOrEmpty(SearchQuery) && !_reportSelectedFilterItems.Any())
        return Report;

      var filterItems = _reportSelectedFilterItems.Any()
        ? Report.Where(o => _reportSelectedFilterItems.Any(a => o.Status == a)).ToList()
        : Report;
      return string.IsNullOrEmpty(SearchQuery)
        ? filterItems
        : filterItems.Where(o => _searchQueryItems.All(a => o.SearchText.ToLower().Contains(a.ToLower()))).ToList();
    }
  }

  public bool HasReportItems =>
    Progress.Report.ReportObjects == null || Progress.Report.ReportObjects.Count == 0 ? false : true;

  public string Log
  {
    get
    {
      string defaultMessage = string.IsNullOrEmpty(Progress.Report.ConversionLogString)
        ? "\nWelcome to the report! \n\nObjects you send or receive will appear here to help you understand how your document has changed."
        : Progress.Report.ConversionLogString;

      string reportInfo =
        $"\nOperation: {(PreviewOn ? "Preview " : "")}{(IsReceiver ? "Received at " : "Sent at ")}{DateTime.Now.ToLocalTime().ToString("dd/MM/yy HH:mm:ss")}";
      reportInfo += $"\nTotal: {Report.Count} objects";
      reportInfo += Progress.Report.OperationErrors.Any()
        ? $"\n\nErrors: \n{Progress.Report.OperationErrorsString}"
        : "";

      return Report.Any() || Progress.Report.OperationErrors.Any() ? reportInfo : defaultMessage;
    }
  }

  private List<string> _searchQueryItems = new();
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
        _searchQueryItems = _searchQuery.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
      this.RaisePropertyChanged(nameof(FilteredReport));
    }
  }

  #region REPORT FILTER

  public SelectionModel<string> ReportSelectionModel { get; set; }
  private List<string> _reportSelectedFilterItems = new();
  private List<string> _reportFilterItems = new();

  public List<string> ReportFilterItems
  {
    get => _reportFilterItems;
    set => this.RaiseAndSetIfChanged(ref _reportFilterItems, value);
  }

  private void ReportFilterSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
  {
    try
    {
      foreach (var a in e.SelectedItems)
        if (!_reportSelectedFilterItems.Contains(a as string))
          _reportSelectedFilterItems.Add(a as string);
      foreach (var r in e.DeselectedItems)
        if (_reportSelectedFilterItems.Contains(r as string))
          _reportSelectedFilterItems.Remove(r as string);

      this.RaisePropertyChanged(nameof(FilteredReport));
    }
    catch (Exception ex) { }
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
          this.RaisePropertyChanged();
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
      this.RaisePropertyChanged(nameof(HasSettings));
    }
  }

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

  private Bitmap _previewImage;

  public Bitmap PreviewImage
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

  private Bitmap _previewImage360;

  public Bitmap PreviewImage360
  {
    get => _previewImage360;
    set => this.RaiseAndSetIfChanged(ref _previewImage360, value);
  }

  private bool _previewImage360Loaded;

  public bool PreviewImage360Loaded
  {
    get => _previewImage360Loaded;
    set => this.RaiseAndSetIfChanged(ref _previewImage360Loaded, value);
  }

  public bool CanOpenCommentsIn3DView { get; set; }
  public bool CanReceive { get; set; }
  public bool HasSettings { get; set; } = true;
  private bool _isAddingBranches;

  #endregion


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
    try
    {
      await GetComments().ConfigureAwait(true);

      var authorName = "you";
      if (e.authorId != Client.Account.userInfo.id)
      {
        var author = await Client.OtherUserGet(e.authorId).ConfigureAwait(true);
        if (author == null)
          authorName = "Unknown";
        else
          authorName = author.name;
      }

      bool openStream = true;
      var svm = MainViewModel.RouterInstance.NavigationStack.Last() as StreamViewModel;
      if (svm != null && svm.Stream.id == Stream.id)
        openStream = false;

      Dispatcher.UIThread.Post(() =>
      {
        MainUserControl.NotificationManager.Show(
          new PopUpNotificationViewModel
          {
            Title = $"🆕 New comment by {authorName}:",
            Message = e.rawText,
            OnClick = () =>
            {
              if (openStream)
                MainViewModel.RouterInstance.Navigate.Execute(this);

              SelectedTab = 3;
            },
            Type = NotificationType.Success,
            Expiration = TimeSpan.FromSeconds(15)
          }
        );
      });
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed to notify of Comment Activity {message}", ex.Message);
    }
  }

  private async void Client_OnBranchChange(object sender, BranchInfo info)
  {
    if (!_isAddingBranches)
      await GetBranches().ConfigureAwait(true);
  }

  private async void Client_OnCommitChange(object sender, CommitInfo info)
  {
    if (info.branchName == SelectedBranch.Branch.name)
      await GetCommits().ConfigureAwait(true);
  }

  private async void Client_OnCommitCreated(object sender, CommitInfo info)
  {
    try
    {
      if (info.branchName == SelectedBranch.Branch.name)
        await GetCommits().ConfigureAwait(true);

      if (!IsReceiver)
        return;

      var authorName = "You";
      if (info.authorId != Client.Account.userInfo.id)
      {
        var author = await Client.OtherUserGet(info.id).ConfigureAwait(true);
        authorName = author.name;
      }

      bool openOnline = false;

      //if in stream edit open online
      var svm = MainViewModel.RouterInstance.NavigationStack.Last() as StreamViewModel;
      if (svm != null && svm.Stream.id == Stream.id)
        openOnline = true;

      Dispatcher.UIThread.Post(() =>
      {
        MainUserControl.NotificationManager.Show(
          new PopUpNotificationViewModel
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
            },
            Type = NotificationType.Success,
            Expiration = TimeSpan.FromSeconds(10)
          }
        );
      });

      ScrollToBottom();

      if (AutoReceive)
        ReceiveCommand();
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(
        ex,
        "Swallowing exception in {methodName}: {exceptionMessage}",
        nameof(Client_OnCommitCreated),
        ex.Message
      );
    }
  }

  private void Client_OnStreamUpdated(object sender, StreamInfo e)
  {
    GetStream().ConfigureAwait(true);
  }

  #endregion


  #region commands

  private const string CommandFailedLogTemplate = "{commandName} failed - {exceptionMessage}";
  private const string CommandSucceededLogTemplate = "{commandName} succeeded";

  private async void AddNewBranch()
  {
    var dialog = new NewBranchDialog();
    var nbvm = new NewBranchViewModel(Branches);
    dialog.DataContext = nbvm;

    var result = await dialog.ShowDialog<bool>().ConfigureAwait(true);

    if (result)
      try
      {
        _isAddingBranches = true;
        var branchId = await StreamState.Client
          .BranchCreate(
            new BranchCreateInput
            {
              streamId = Stream.id,
              description = nbvm.Description ?? "",
              name = nbvm.BranchName
            }
          )
          .ConfigureAwait(true);
        await GetBranches().ConfigureAwait(true);

        var index = Branches.FindIndex(x => x.name == nbvm.BranchName);
        if (index != -1)
          SelectedBranch = BranchesViewModel[index];

        Analytics.TrackEvent(
          Analytics.Events.DUIAction,
          new Dictionary<string, object> { { "name", "Branch Create" } }
        );
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Failed adding new branch {exceptionMessage}", ex.Message);
        Dialogs.ShowDialog("Something went wrong...", ex.Message, DialogIconKind.Error);
      }
      finally
      {
        _isAddingBranches = false;
      }
    else
      //make sure the a branch is selected if canceled
      SelectedBranch = BranchesViewModel[0];
  }

  public async void CopyReportCommand()
  {
    var reportObjectSummaries = FilteredReport.Select(o => o.GetSummary()).ToArray();
    var summary = string.Join("\n", reportObjectSummaries);

    await Application.Current.Clipboard.SetTextAsync(summary).ConfigureAwait(true);
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Copy Report" } });
  }

  public void ClearSearchCommand()
  {
    SearchQuery = "";
  }

  public void ShareCommand()
  {
    MainViewModel.RouterInstance.Navigate.Execute(new CollaboratorsViewModel(HostScreen, this));

    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Share Open" } });
  }

  public void EditSavedStreamCommand()
  {
    MainViewModel.RouterInstance.Navigate.Execute(this);
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Stream Edit" } });
  }

  public async void ViewOnlineSavedStreamCommand()
  {
    //ensure click transition has finished
    await Task.Delay(100).ConfigureAwait(true);

    OpenUrl(Url);
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Stream View" } });
  }

  private void OpenUrl(string url)
  {
    //to open urls in .net core must set UseShellExecute = true
    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
  }

  public async void CopyStreamURLCommand()
  {
    //ensure click transition has finished
    await Task.Delay(100).ConfigureAwait(true);
    Application.Current.Clipboard.SetTextAsync(Url);
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Stream Copy Link" } });
  }

  public async void SendCommand()
  {
    try
    {
      UpdateStreamState();
      ResetProgress();
      Progress.IsProgressing = true;

      if (!await Http.UserHasInternet().ConfigureAwait(true))
        throw new InvalidOperationException("Could not reach the internet, are you connected?");

      Progress.CancellationToken.ThrowIfCancellationRequested();

      // We don't pass the progress cancellation token into Task.Run, as forcefully ending the task could leave a host app in an invalid state. Instead ConnectorBindings should Token.ThrowIfCancellationRequested when it's safe.
      var commitId = await Task.Run(() => Bindings.SendStream(StreamState, Progress)).ConfigureAwait(true);

      if (commitId == null)
      {
        // Ideally, commitId shouldn't return null, as we have no context WHY, or if the application is left in an invalid state.
        // This is a last ditch effort to display a semi-useful error to the user.
        string message = Progress?.Report?.OperationErrorsString;
        if (string.IsNullOrEmpty(message))
          message = "Something went very wrong";
        throw new Exception(message);
      }

      LastUsedTime = DateTime.UtcNow;
      var view = MainViewModel.RouterInstance.NavigationStack.Last() is StreamViewModel ? "Stream" : "Home";

      Analytics.TrackEvent(
        Client.Account,
        Analytics.Events.Send,
        new Dictionary<string, object>
        {
          { "filter", StreamState.Filter.Name },
          { "view", view },
          { "collaborators", Stream.collaborators.Count },
          { "isMain", SelectedBranch.Branch.name == "main" ? true : false },
          { "branches", Stream.branches?.totalCount },
          { "commits", Stream.commits?.totalCount },
          { "savedStreams", HomeViewModel.Instance.SavedStreams?.Count }
        }
      );

      DisplayPopupNotification(
        new PopUpNotificationViewModel
        {
          Title = "👌 Data sent",
          Message = $"Sent to '{Stream.name}', view it online",
          OnClick = () =>
          {
            var url = $"{StreamState.ServerUrl}/streams/{StreamState.StreamId}/commits/{commitId}";
            var config = ConfigManager.Load();
            if (config.UseFe2)
              url = $"{StreamState.ServerUrl}/projects/{StreamState.StreamId}/models/{SelectedBranch.Branch.id}";
            OpenUrl(url);
          },
          Type = NotificationType.Success,
          Expiration = TimeSpan.FromSeconds(10)
        }
      );

      GetActivity();
      GetReport();

      //save the stream as well
      HomeViewModel.Instance.AddSavedStream(this);

      SpeckleLog.Logger.Information(CommandSucceededLogTemplate, nameof(SendCommand));
    }
    catch (Exception ex)
    {
      HandleCommandException(ex);
    }
    finally
    {
      Progress.CancellationTokenSource.Cancel();
      Progress.IsProgressing = false;
      Progress.Value = 0;
      Progress.Max = 1;
    }
  }

  public async void PreviewCommand()
  {
    PreviewOn = !PreviewOn;
    if (!PreviewOn)
    {
      Progress?.CancellationTokenSource.Cancel();
      Bindings.ResetDocument();
      return;
    }

    try
    {
      UpdateStreamState();
      ResetProgress();
      Progress.IsPreviewProgressing = true;

      string previewName = IsReceiver ? "Preview Receive" : "Preview Send";

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", previewName } });

      if (IsReceiver)
        await Task.Run(() => Bindings.PreviewReceive(StreamState, Progress)).ConfigureAwait(true);
      else
        //NOTE: do not wrap in a Task or it will crash Revit
        Bindings.PreviewSend(StreamState, Progress);

      GetReport();
      SpeckleLog.Logger
        .ForContext(nameof(IsReceiver), IsReceiver)
        .Information(CommandSucceededLogTemplate, nameof(PreviewCommand));
    }
    catch (Exception ex)
    {
      HandleCommandException(ex);
    }
    finally
    {
      Progress.CancellationTokenSource.Cancel();
      Progress.IsPreviewProgressing = false;
      Progress.Value = 0;
      Progress.Max = 1;
    }
  }

  public async void ReceiveCommand()
  {
    try
    {
      UpdateStreamState();
      ResetProgress();
      Progress.IsProgressing = true;

      if (!await Http.UserHasInternet().ConfigureAwait(true))
        throw new InvalidOperationException("Could not reach the internet, are you connected?");

      Progress.CancellationToken.ThrowIfCancellationRequested();

      //NOTE: We don't pass the cancellation token into Task.Run, as forcefully ending the task could leave a host app in an invalid state. Instead ConnectorBindings should Token.ThrowIfCancellationRequested when it's safe.
      var state = await Task.Run(() => Bindings.ReceiveStream(StreamState, Progress)).ConfigureAwait(true);

      if (state == null)
      {
        //NOTE: Ideally, ReceiveStream shouldn't return null, as we have no context WHY, or if the application is left in an invalid state.
        // This is a last ditch effort to display a semi-useful error to the user.
        string message = Progress?.Report?.OperationErrorsString;
        if (string.IsNullOrEmpty(message))
          message = "Something went very wrong";
        throw new Exception(message);
      }

      // Track receive operation
      var view = MainViewModel.RouterInstance.NavigationStack.Last() is StreamViewModel ? "Stream" : "Home";
      LastUsedTime = DateTime.UtcNow;

      Analytics.TrackEvent(
        StreamState.Client.Account,
        Analytics.Events.Receive,
        new Dictionary<string, object>
        {
          { "mode", StreamState.ReceiveMode },
          { "auto", StreamState.AutoReceive },
          { "sourceHostApp", HostApplications.GetHostAppFromString(state.LastCommit?.sourceApplication).Slug },
          { "sourceHostAppVersion", state.LastCommit?.sourceApplication },
          { "view", view },
          { "collaborators", Stream.collaborators.Count },
          { "isMain", SelectedBranch.Branch.name == "main" ? true : false },
          { "branches", Stream.branches?.totalCount },
          { "commits", Stream.commits?.totalCount },
          { "savedStreams", HomeViewModel.Instance.SavedStreams?.Count },
          { "isMultiplayer", state.LastCommit != null ? state.LastCommit.authorId != state.UserId : false }
        }
      );

      // Show report
      GetActivity();
      GetReport();

      // Save the stream
      HomeViewModel.Instance.AddSavedStream(this);

      // Display success message
      string successMessage = "";

      var warningsCount = Progress.Report.OperationErrors.Count + Progress.Report.ConversionErrors.Count;
      if (warningsCount > 0)
        successMessage = $"There were {warningsCount} warning(s)";
      else if (Progress.CancellationToken.IsCancellationRequested)
        // User requested a cancel, but it was too late!
        successMessage = "It was too late to cancel";

      DisplayPopupNotification(
        new PopUpNotificationViewModel
        {
          Title = "👌 Receive completed!",
          Message = successMessage,
          Type = NotificationType.Success
        }
      );

      SpeckleLog.Logger.Information(CommandSucceededLogTemplate, nameof(ReceiveCommand));
    }
    catch (Exception ex)
    {
      HandleCommandException(ex);
    }
    finally
    {
      Progress.CancellationTokenSource.Cancel();
      Progress.IsProgressing = false;
      Progress.Value = 0;
      Progress.Max = 1;
    }
  }

  private void HandleCommandException(Exception ex, [CallerMemberName] string commandName = "UnknownCommand")
  {
    HandleCommandException(ex, Progress.CancellationToken.IsCancellationRequested, commandName);
  }

  public static void HandleCommandException(
    Exception ex,
    bool isUserCancel,
    [CallerMemberName] string commandName = "UnknownCommand"
  )
  {
    string commandPrettyName = commandName.EndsWith("Command")
      ? commandName.Substring(0, commandName.Length - "Command".Length)
      : commandName;

    LogEventLevel logLevel;
    INotification notificationViewModel;
    switch (ex)
    {
      case OperationCanceledException:
        // NOTE: We expect an OperationCanceledException to occur when our CancellationToken is cancelled.
        // If our token wasn't cancelled, then this is highly unexpected, and treated with HIGH SEVERITY!
        // Likely, another deeper token was cancelled, and the exception wasn't handled correctly somewhere deeper.

        logLevel = isUserCancel ? LogEventLevel.Information : LogEventLevel.Error;
        notificationViewModel = new PopUpNotificationViewModel
        {
          Title = $"✋ {commandPrettyName} cancelled!",
          Message = isUserCancel ? "Operation canceled" : ex.Message,
          Type = isUserCancel ? NotificationType.Success : NotificationType.Error
        };
        break;
      case InvalidOperationException:
        // NOTE: Hopefully, this means that the Receive/Send/Preview/etc. command could not START because of invalid state
        logLevel = LogEventLevel.Warning;
        notificationViewModel = new PopUpNotificationViewModel
        {
          Title = $"❌ {commandPrettyName} cancelled!", // InvalidOperation implies we didn't even try to complete the command, therefore "cancelled" rather than "failed"
          Message = ex.Message,
          Type = NotificationType.Warning
        };
        break;
      case SpeckleGraphQLException<StreamData>:
        // NOTE: GraphQL requests for StreamData are expected to fail for a variety of reasons
        logLevel = LogEventLevel.Warning;
        notificationViewModel = new PopUpNotificationViewModel
        {
          Title = $"😞 {commandPrettyName} Failed!",
          Message = $"Failed to fetch stream data from server. Reason: {ex.Message}",
          Type = NotificationType.Error
        };
        break;
      case SpeckleNonUserFacingException:
        logLevel = LogEventLevel.Error;
        notificationViewModel = new PopUpNotificationViewModel
        {
          Title = $"😖 {commandPrettyName} Failed!",
          Message = "Click to open the log file for a detailed list of error messages",
          OnClick = SpeckleLog.OpenCurrentLogFolder,
          Type = NotificationType.Error,
          Expiration = TimeSpan.FromSeconds(10)
        };
        break;
      case SpeckleException:
        logLevel = LogEventLevel.Error;
        notificationViewModel = new PopUpNotificationViewModel
        {
          Title = $"😖 {commandPrettyName} Failed!",
          Message = ex.Message,
          Type = NotificationType.Error
        };
        break;
      default:
        // Unexpected exceptions are treated with highest severity, as we don't know if the application was left in an invalid state
        // All fatal exceptions should be investigated, and either FIXED or REPORTED differently.
        logLevel = LogEventLevel.Fatal;
        notificationViewModel = new PopUpNotificationViewModel
        {
          Title = $"💥 {commandPrettyName} Error!",
          Message = $"{ex.GetType()} - {ex.Message}",
          Type = NotificationType.Error
        };
        break;
    }

    DisplayPopupNotification(notificationViewModel);
    SpeckleLog.Logger.Write(logLevel, ex, CommandFailedLogTemplate, commandName, ex.Message);
  }

  private static void DisplayPopupNotification(INotification notification)
  {
    Dispatcher.UIThread.Post(
      () => MainUserControl.NotificationManager.Show(notification),
      DispatcherPriority.Background
    );
  }

  private void ResetProgress()
  {
    Progress = new ProgressViewModel();
  }

  public void CancelSendOrReceiveCommand()
  {
    Progress.CancellationTokenSource.Cancel();

    string cancelledEvent = IsReceiver ? "Cancel Receive" : "Cancel Send";
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", cancelledEvent } });

    //NOTE: We don't want to show a notification yet! Just because cancellation is requested, doesn't mean the receive operation has been cancelled yet.
  }

  public void CancelPreviewCommand()
  {
    Progress.CancellationTokenSource.Cancel();
    string cancelledEvent = IsReceiver ? "Cancel Preview Receive" : "Cancel Preview Send";
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", cancelledEvent } });
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
        Analytics.TrackEvent(
          Client.Account,
          Analytics.Events.DUIAction,
          new Dictionary<string, object> { { "name", "Stream Receiver Add" } }
        );
      else
        Analytics.TrackEvent(
          Client.Account,
          Analytics.Events.DUIAction,
          new Dictionary<string, object> { { "name", "Stream Sender Add" } }
        );

      MainUserControl.NotificationManager.Show(
        new PopUpNotificationViewModel
        {
          Title = "💾 Stream Saved",
          Message = "This stream has been saved to this file",
          Type = NotificationType.Success
        }
      );
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(
        ex,
        "Unexpected exception in {commandName} {exceptionMessage}",
        nameof(SaveCommand),
        ex.Message
      );
    }
  }

  private void OpenSettingsCommand()
  {
    try
    {
      var settingsPageViewModel = new SettingsPageViewModel(
        HostScreen,
        Settings.Select(x => new SettingViewModel(x)).ToList(),
        this
      );
      MainViewModel.RouterInstance.Navigate.Execute(settingsPageViewModel);
      Analytics.TrackEvent(
        StreamState.Client.Account,
        Analytics.Events.DUIAction,
        new Dictionary<string, object> { { "name", "Settings Open" } }
      );
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(
        ex,
        "Unexpected exception in {commandName} {exceptionMessage}",
        nameof(OpenSettingsCommand),
        ex.Message
      );
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

  [
    DependsOn(nameof(SelectedBranch)),
    DependsOn(nameof(SelectedFilter)),
    DependsOn(nameof(SelectedCommit)),
    DependsOn(nameof(IsReceiver))
  ]
  private bool CanSaveCommand(object parameter)
  {
    return true;
  }

  [DependsOn(nameof(SelectedBranch)), DependsOn(nameof(SelectedFilter)), DependsOn(nameof(IsReceiver))]
  private bool CanSendCommand(object parameter)
  {
    return IsReady();
  }

  [DependsOn(nameof(SelectedBranch)), DependsOn(nameof(SelectedCommit)), DependsOn(nameof(IsReceiver))]
  private bool CanReceiveCommand(object parameter)
  {
    return IsReady();
  }

  [
    DependsOn(nameof(SelectedBranch)),
    DependsOn(nameof(SelectedCommit)),
    DependsOn(nameof(SelectedFilter)),
    DependsOn(nameof(IsReceiver))
  ]
  private bool CanPreviewCommand(object parameter)
  {
    bool previewImplemented = IsReceiver ? Bindings.CanPreviewReceive : Bindings.CanPreviewSend;
    if (previewImplemented)
      return IsReady();
    return false;
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
    Client?.Dispose();
  }

  public async Task DownloadImage(Uri url)
  {
    throw new NotImplementedException();
  }

  #endregion
}
