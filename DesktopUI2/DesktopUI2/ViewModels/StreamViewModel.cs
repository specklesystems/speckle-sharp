﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels.Share;
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
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Stream = Speckle.Core.Api.Stream;

namespace DesktopUI2.ViewModels
{
  public class StreamViewModel : ReactiveObject, IRoutableViewModel
  {

    public StreamState StreamState { get; set; }
    public IScreen HostScreen { get; set; }

    private ConnectorBindings Bindings;

    private List<MenuItemViewModel> _menuItems = new List<MenuItemViewModel>();

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

    #region bindings
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

    public string NotificationUrl { get; set; }

    private string _notification;
    public string Notification
    {
      get => _notification;
      set
      {
        this.RaiseAndSetIfChanged(ref _notification, value);
        this.RaisePropertyChanged("ShowNotification");
      }
    }

    public bool ShowNotification
    {
      get => !string.IsNullOrEmpty(Notification);
    }

    private bool _isRemovingStream;
    public bool IsRemovingStream
    {
      get => _isRemovingStream;
      private set
      {
        this.RaiseAndSetIfChanged(ref _isRemovingStream, value);
      }
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

    public string UrlPathSegment { get; } = "stream";

    private Client Client { get; }

    public ReactiveCommand<Unit, Unit> GoBack
    {
      get
      {
        PreviewOn = false;
        Bindings.ResetDocument();
        return MainViewModel.RouterInstance.NavigateBack;
      }
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
            PreviewImageUrl = Client.Account.serverInfo.url + $"/preview/{Stream.id}/branches/{SelectedBranch.Branch.name}";
          else
            PreviewImageUrl = Client.Account.serverInfo.url + $"/preview/{Stream.id}/commits/{_selectedCommit.id}";
        }
      }
    }

    private List<Commit> _commits;
    public List<Commit> Commits
    {
      get => _commits;
      private set
      {
        this.RaiseAndSetIfChanged(ref _commits, value);
        this.RaisePropertyChanged("HasCommits");
      }
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
    public bool HasCommits => Commits != null && Commits.Any();

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

    #endregion

    private string Url
    {
      get
      {
        //sender
        if (!IsReceiver)
        {
          if (SelectedBranch != null && SelectedBranch.Branch.name != "main")
            return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{SelectedBranch.Branch.name}";
        }
        //receiver
        else
        {
          if (SelectedCommit != null && SelectedCommit.id != "latest")
            return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/commits/{SelectedCommit.id}";
          if (SelectedBranch != null)
            return $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{SelectedBranch.Branch.name}";
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
    public StreamViewModel(StreamState streamState, IScreen hostScreen, ICommand removeSavedStreamCommand)
    {
      try
      {
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

        //use dependency injection to get bindings
        Bindings = Locator.Current.GetService<ConnectorBindings>();

        if (Client == null)
        {
          NoAccess = true;
          Notification = "You do not have access to this Stream.";
          NotificationUrl = $"{streamState.ServerUrl}/streams/{streamState.StreamId}";
          return;
        }

        Init();
        GenerateMenuItems();

        var updateTextTimer = new System.Timers.Timer();
        updateTextTimer.Elapsed += UpdateTextTimer_Elapsed;
        updateTextTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
        updateTextTimer.Enabled = true;
      }
      catch (Exception ex)
      {

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
        var menu = new MenuItemViewModel { Header = new MaterialIcon { Kind = MaterialIconKind.EllipsisVertical, Foreground = Avalonia.Media.Brushes.Gray } };
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

      }
    }

    public async Task GetStream()
    {
      try
      {
        Stream = await Client.StreamGet(StreamState.StreamId);
        StreamState.CachedStream = Stream;

        //subscription
        Client.SubscribeCommitCreated(StreamState.StreamId);
        Client.OnCommitCreated += Client_OnCommitCreated;
      }
      catch (Exception e)
      {
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
        var tabControl = StreamEditView.Instance.FindControl<TabControl>("tabStreamEdit");
        tabControl.SelectedIndex = tabControl.ItemCount - 1;
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

      }
    }

    private async void GetComments()
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

      }
    }

    private async void GetCommits()
    {
      try
      {
        if (SelectedBranch.Branch.commits == null || SelectedBranch.Branch.commits.totalCount > 0)
        {
          var branch = await Client.BranchGet(Stream.id, SelectedBranch.Branch.name, 100);
          branch.commits.items.Insert(0, new Commit { id = "latest", message = "Always receive the latest commit sent to this branch." });
          Commits = branch.commits.items;
          var commit = Commits.FirstOrDefault(x => x.id == StreamState.CommitId);
          if (commit != null)
            SelectedCommit = commit;
          else
            SelectedCommit = Commits[0];
        }
        else
        {
          SelectedCommit = null;
          Commits = new List<Commit>();
          SelectedCommit = null;
        }
      }
      catch (Exception ex)
      {

      }
    }

    private async void Client_OnCommitCreated(object sender, Speckle.Core.Api.SubscriptionModels.CommitInfo info)
    {
      try
      {
        var branches = await Client.StreamGetBranches(StreamState.StreamId);

        if (!IsReceiver) return;

        var binfo = branches.FirstOrDefault(b => b.name == info.branchName);
        var cinfo = binfo.commits.items.FirstOrDefault(c => c.id == info.id);

        Notification = $"{cinfo.authorName} sent to {info.branchName}: {info.message}";
        NotificationUrl = $"{StreamState.ServerUrl}/streams/{StreamState.StreamId}/commits/{cinfo.id}";
        ScrollToBottom();

        if (AutoReceive)
          ReceiveCommand();
      }
      catch (Exception ex)
      {

      }
    }

    public void DownloadImage(string url)
    {
      try
      {
        using (WebClient client = new WebClient())
        {
          client.Headers.Set("Authorization", "Bearer " + Client.ApiToken);
          client.DownloadDataAsync(new Uri(url));
          client.DownloadDataCompleted += DownloadComplete;
        }
      }
      catch (Exception ex)
      {

      }
    }

    private void DownloadComplete(object sender, DownloadDataCompletedEventArgs e)
    {
      try
      {
        byte[] bytes = e.Result;

        System.IO.Stream stream = new MemoryStream(bytes);

        var image = new Avalonia.Media.Imaging.Bitmap(stream);
        _previewImage = image;
        this.RaisePropertyChanged("PreviewImage");
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(ex);
        PreviewImageUrl = null; // Could not download...
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

          var branchId = await this.StreamState.Client.BranchCreate(new BranchCreateInput { streamId = Stream.id, description = nbvm.Description ?? "", name = nbvm.BranchName });


          Branches = await Client.StreamGetBranches(Stream.id, 100, 0);

          var index = Branches.FindIndex(x => x.name == nbvm.BranchName);
          if (index != -1)
            SelectedBranch = BranchesViewModel[index];

        }
        catch (Exception e)
        {
          Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
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
    }

    public void CloseNotificationCommand()
    {
      Notification = "";
      NotificationUrl = "";
      Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Notification Dismiss" } });
    }

    public void LaunchNotificationCommand()
    {
      Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Notification Click" } });

      if (!string.IsNullOrEmpty(NotificationUrl))
        Process.Start(new ProcessStartInfo(NotificationUrl) { UseShellExecute = true });

      CloseNotificationCommand();
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
      //to open urls in .net core must set UseShellExecute = true
      Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream View" } });
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

        Progress.IsProgressing = true;
        var commitId = await Task.Run(() => Bindings.SendStream(StreamState, Progress));
        Progress.IsProgressing = false;

        if (!Progress.CancellationTokenSource.IsCancellationRequested && commitId != null)
        {
          LastUsed = DateTime.Now.ToString();
          Analytics.TrackEvent(Client.Account, Analytics.Events.Send, new Dictionary<string, object> { { "filter", StreamState.Filter.Name } });

          Notification = $"Sent successfully, view online";
          NotificationUrl = $"{StreamState.ServerUrl}/streams/{StreamState.StreamId}/commits/{commitId}";
        }
        else
        {
          Notification = "Nothing sent!";
        }

        GetActivity();
        GetReport();
      }
      catch (Exception ex)
      {

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
        Progress.IsProgressing = true;
        await Task.Run(() => Bindings.ReceiveStream(StreamState, Progress));
        Progress.IsProgressing = false;

        if (!Progress.CancellationTokenSource.IsCancellationRequested)
        {
          LastUsed = DateTime.Now.ToString();
          Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.Receive, new Dictionary<string, object>() { { "mode", StreamState.ReceiveMode }, { "auto", StreamState.AutoReceive } });
        }

        GetActivity();
        GetReport();
      }
      catch (Exception ex)
      {

      }
    }

    private void Reset()
    {
      Notification = "";
      NotificationUrl = "";
      Progress = new ProgressViewModel();
    }

    public void CancelSendOrReceiveCommand()
    {
      Progress.CancellationTokenSource.Cancel();
      Reset();
      string cancelledEvent = IsReceiver ? "Cancel Receive" : "Cancel Send";
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", cancelledEvent } });
      Notification = IsReceiver ? "Cancelled Receive" : "Cancelled Send";
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
      }
      catch (Exception ex)
      {

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
    #endregion

  }
}
