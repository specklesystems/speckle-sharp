﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.Views;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Windows;
using DynamicData;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using Speckle.Core.Api;
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
    private IScreen HostScreen { get; set; }

    private ConnectorBindings Bindings;

    private List<MenuItemViewModel> _menuItems = new List<MenuItemViewModel>();

    public ICommand RemoveSavedStreamCommand { get; }

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

    private bool _showReport;

    public bool ShowReport
    {
      get => _showReport;
      private set
      {
        this.RaiseAndSetIfChanged(ref _showReport, value);
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




    public ReactiveCommand<Unit, Unit> GoBack => MainWindowViewModel.RouterInstance.NavigateBack;

    //If we don't have access to this stream
    public bool NoAccess { get; set; } = false;

    private bool _isReceiver = false;
    public bool IsReceiver
    {
      get => _isReceiver;
      set
      {
        this.RaiseAndSetIfChanged(ref _isReceiver, value);
      }
    }



    private Branch _selectedBranch;
    public Branch SelectedBranch
    {
      get => _selectedBranch;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedBranch, value);
        if (value != null)
          GetCommits();
      }
    }

    private List<Branch> _branches;
    public List<Branch> Branches
    {
      get => _branches;
      private set => this.RaiseAndSetIfChanged(ref _branches, value);
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
            PreviewImageUrl = Client.Account.serverInfo.url + $"/preview/{Stream.id}";
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

    private FilterViewModel _selectedFilter;
    public FilterViewModel SelectedFilter
    {
      get => _selectedFilter;
      set
      {
        //trigger change when any property in the child model view changes
        //used for the CanSave etc button bindings
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

    private List<ISetting> _settings;
    public List<ISetting> Settings
    {
      get => _settings;
      private set
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

    private string Url { get => $"{StreamState.ServerUrl.TrimEnd('/')}/streams/{StreamState.StreamId}/branches/{StreamState.BranchName}"; }

    IScreen IRoutableViewModel.HostScreen => throw new NotImplementedException();

    public void UpdateHost(IScreen hostScreen)
    {
      HostScreen = hostScreen;
    }
    public StreamViewModel(StreamState streamState, IScreen hostScreen, ICommand removeSavedStreamCommand)

    {
      StreamState = streamState;
      //use cached stream, then load a fresh one async 
      //this way we can immediately show stream name and other info and update it later if it changed
      Stream = streamState.CachedStream;
      Client = streamState.Client;
      IsReceiver = streamState.IsReceiver;

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

      GetStream().ConfigureAwait(false);
      GenerateMenuItems();

      //get available filters from our bindings
      AvailableFilters = new List<FilterViewModel>(Bindings.GetSelectionFilters().Select(x => new FilterViewModel(x)));
      SelectedFilter = AvailableFilters[0];

      //get available settings from our bindings
      Settings = Bindings.GetSettings();

      GetBranchesAndRestoreState();
      GetActivity();


      var updateTextTimer = new System.Timers.Timer();
      updateTextTimer.Elapsed += UpdateTextTimer_Elapsed;
      updateTextTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
      updateTextTimer.Enabled = true;
    }

    private void UpdateTextTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      this.RaisePropertyChanged("LastUsed");
      this.RaisePropertyChanged("LastUpdated");
    }

    private void GenerateMenuItems()
    {
      var menu = new MenuItemViewModel { Header = new MaterialIcon { Kind = MaterialIconKind.EllipsisVertical, Foreground = Avalonia.Media.Brushes.Gray } };
      menu.Items = new List<MenuItemViewModel> {
        //new MenuItemViewModel (EditSavedStreamCommand, "Edit",  MaterialIconKind.Cog),
        new MenuItemViewModel (ViewOnlineSavedStreamCommand, "View online",  MaterialIconKind.ExternalLink),
        new MenuItemViewModel (CopyStreamURLCommand, "Copy URL to clipboard",  MaterialIconKind.ContentCopy),
        new MenuItemViewModel (OpenReportCommand, "Open Report",  MaterialIconKind.TextBox)
      };
      var customMenues = Bindings.GetCustomStreamMenuItems();
      if (customMenues != null)
        menu.Items.AddRange(customMenues.Select(x => new MenuItemViewModel(x, this.StreamState)).ToList());
      //remove is added last
      //menu.Items.Add(new MenuItemViewModel(RemoveSavedStreamCommand, StreamState.Id, "Remove", MaterialIconKind.Bin));
      MenuItems.Add(menu);

      this.RaisePropertyChanged("MenuItems");
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

    private async void GetBranchesAndRestoreState()
    {
      var branches = await Client.StreamGetBranches(Stream.id, 100, 0);
      Branches = branches;

      var branch = Branches.FirstOrDefault(x => x.name == StreamState.BranchName);
      if (branch != null)
        SelectedBranch = branch;
      else
        SelectedBranch = Branches[0];

      if (StreamState.Filter != null)
      {
        SelectedFilter = AvailableFilters.FirstOrDefault(x => x.Filter.Slug == StreamState.Filter.Slug);
        if (SelectedFilter != null)
          SelectedFilter.Filter = StreamState.Filter;
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

    private async void GetActivity()
    {

      var filteredActivity = (await Client.StreamGetActivity(Stream.id))
        .Where(x => x.actionType == "commit_create" || x.actionType == "commit_receive" || x.actionType == "stream_create")
        .Reverse().ToList();
      var activity = new List<ActivityViewModel>();
      foreach (var a in filteredActivity)
      {
        var avm = new ActivityViewModel();
        await avm.Init(a, Client);
        activity.Add(avm);

      }
      Activity = activity;

      if (StreamEditView.Instance != null)
      {
        var scroller = StreamEditView.Instance.FindControl<ScrollViewer>("activityScroller");
        if (scroller != null)
        {
          await Task.Delay(250);
          scroller.ScrollToEnd();
        }
      }
    }

    /// <summary>
    /// Update the model Stream state whenever we send, receive or save a stream
    /// </summary>
    private void UpdateStreamState()
    {
      StreamState.BranchName = SelectedBranch.name;
      StreamState.IsReceiver = IsReceiver;
      if (IsReceiver)
        StreamState.CommitId = SelectedCommit.id;
      if (!IsReceiver)
        StreamState.Filter = SelectedFilter.Filter;
      StreamState.Settings = Settings.Select(o => o).ToList();
    }

    private async void GetCommits()
    {
      if (SelectedBranch.commits == null || SelectedBranch.commits.totalCount > 0)
      {
        var branch = await Client.BranchGet(Stream.id, SelectedBranch.name, 100);
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


    private async void Client_OnCommitCreated(object sender, Speckle.Core.Api.SubscriptionModels.CommitInfo info)
    {
      var branches = await Client.StreamGetBranches(StreamState.StreamId);

      if (!IsReceiver) return;

      var binfo = branches.FirstOrDefault(b => b.name == info.branchName);
      var cinfo = binfo.commits.items.FirstOrDefault(c => c.id == info.id);

      Notification = $"{cinfo.authorName} sent to {info.branchName}: {info.message}";
      NotificationUrl = $"{StreamState.ServerUrl}/streams/{StreamState.StreamId}/commits/{cinfo.id}";
    }


    public void DownloadImage(string url)
    {
      using (WebClient client = new WebClient())
      {
        client.Headers.Set("Authorization", "Bearer " + Client.ApiToken);
        client.DownloadDataAsync(new Uri(url));
        client.DownloadDataCompleted += DownloadComplete;
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
    public void CloseNotificationCommand()
    {
      Notification = "";
      NotificationUrl = "";
      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Notification Dismiss" } });
    }

    public void CloseReportNotificationCommand()
    {
      ShowReport = false;
      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Report Dismiss" } });
    }


    public void LaunchNotificationCommand()
    {
      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Notification Click" } });

      Process.Start(new ProcessStartInfo(NotificationUrl) { UseShellExecute = true });

      CloseNotificationCommand();
    }

    public void EditSavedStreamCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(this);
      Tracker.TrackPageview("stream", "edit");
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Edit" } });
    }

    public async void ViewOnlineSavedStreamCommand()
    {
      //ensure click transition has finished
      await Task.Delay(100);
      //to open urls in .net core must set UseShellExecute = true
      Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
      Tracker.TrackPageview(Tracker.STREAM_VIEW);
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
      UpdateStreamState();
      //save the stream as well
      HomeViewModel.Instance.AddSavedStream(this);

      Reset();
      Progress.IsProgressing = true;
      var commitId = await Task.Run(() => Bindings.SendStream(StreamState, Progress));
      Progress.IsProgressing = false;

      if (!Progress.CancellationTokenSource.IsCancellationRequested)
      {
        LastUsed = DateTime.Now.ToString();
        Analytics.TrackEvent(Client.Account, Analytics.Events.Send);
        Tracker.TrackPageview(Tracker.SEND);

        Notification = $"Sent successfully, view online";
        NotificationUrl = $"{StreamState.ServerUrl}/streams/{StreamState.StreamId}/commits/{commitId}";
      }

      if (Progress.Report.ConversionErrorsCount > 0 || Progress.Report.OperationErrorsCount > 0)
        ShowReport = true;

      GetActivity();
    }

    public async void ReceiveCommand()
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
        Analytics.TrackEvent(StreamState.Client.Account, Analytics.Events.Receive);
        Tracker.TrackPageview(Tracker.RECEIVE);
      }

      if (Progress.Report.ConversionErrorsCount > 0 || Progress.Report.OperationErrorsCount > 0)
        ShowReport = true;


      GetActivity();

    }

    private void Reset()
    {
      Notification = "";
      NotificationUrl = "";
      ShowReport = false;
      Progress = new ProgressViewModel();
    }

    public void CancelSendOrReceiveCommand()
    {
      Progress.CancellationTokenSource.Cancel();
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Cancel Send or Receive" } });
    }

    public async void OpenReportCommand()
    {
      //ensure click transition has finished
      await Task.Delay(1000);
      ShowReport = true;
      var report = new Report();
      report.Title = $"Report of the last operation, {LastUsed.ToLower()}";
      report.DataContext = Progress;
      report.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
      report.ShowDialog(MainWindow.Instance);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Open Report" } });
    }



    private void SaveCommand()
    {
      UpdateStreamState();
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      HomeViewModel.Instance.AddSavedStream(this);

      if (IsReceiver)
      {
        Tracker.TrackPageview(Tracker.RECEIVE_ADDED);
        Analytics.TrackEvent(Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Receiver Add" } });
      }

      else
      {
        Tracker.TrackPageview(Tracker.SEND_ADDED);
        Analytics.TrackEvent(Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Sender Add" } });
      }
    }

    //private async void SendCommand()
    //{

    //  try
    //  {
    //    Progress = new ProgressViewModel();
    //    Progress.ProgressTitle = "Sending to Speckle 🚀";
    //    Progress.IsProgressing = true;

    //    var dialog = new QuickOpsDialog();
    //    dialog.DataContext = Progress;
    //    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    //    dialog.ShowDialog(MainWindow.Instance);
    //    await Task.Run(() => Bindings.SendStream(GetStreamState(), Progress));
    //    Progress.IsProgressing = false;

    //    if (!Progress.CancellationTokenSource.IsCancellationRequested)
    //    {
    //      Analytics.TrackEvent(Client.Account, Analytics.Events.Send, new Dictionary<string, object>() { { "method", "Quick" } });
    //      Tracker.TrackPageview(Tracker.SEND);
    //    }
    //    else
    //      dialog.Close(); // if user cancelled close automatically

    //    MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);

    //  }
    //  catch (Exception ex)
    //  {

    //  }
    //}

    //private async void ReceiveCommand()
    //{
    //  try
    //  {
    //    Progress = new ProgressViewModel();
    //    Progress.ProgressTitle = "Receiving from Speckle 🚀";
    //    Progress.IsProgressing = true;

    //    var dialog = new QuickOpsDialog();
    //    dialog.DataContext = Progress;
    //    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    //    dialog.ShowDialog(MainWindow.Instance);

    //    await Task.Run(() => Bindings.ReceiveStream(GetStreamState(), Progress));

    //    Progress.IsProgressing = false;

    //    if (!Progress.CancellationTokenSource.IsCancellationRequested)
    //    {
    //      Analytics.TrackEvent(Client.Account, Analytics.Events.Receive, new Dictionary<string, object>() { { "method", "Quick" } });
    //      Tracker.TrackPageview(Tracker.RECEIVE);
    //    }
    //    else
    //      dialog.Close(); // if user cancelled close automatically

    //    MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
    //  }
    //  catch (Exception ex)
    //  {

    //  }
    //}



    private async void OpenSettingsCommand()
    {
      try
      {
        var settingsWindow = new Settings();
        settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // Not doing this causes Avalonia to throw an error about the owner being already set on the Setting View UserControl
        Settings.ForEach(x => x.ResetView());

        var settingsPageViewModel = new SettingsPageViewModel(Settings.Select(x => new SettingViewModel(x)).ToList());
        settingsWindow.DataContext = settingsPageViewModel;
        settingsWindow.Title = $"Settings for {Stream.name}";
        Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Settings Open" } });
        var saveResult = await settingsWindow.ShowDialog<bool?>(MainWindow.Instance); // TODO: debug throws "control already has a visual parent exception" when calling a second time

        if (saveResult != null && (bool)saveResult)
        {
          Settings = settingsPageViewModel.Settings.Select(x => x.Setting).ToList();
        }
      }
      catch (Exception e)
      {
      }
    }


    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedFilter))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSaveCommand(object parameter)
    {
      return IsReady();
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
