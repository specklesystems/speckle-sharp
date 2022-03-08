using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class StreamEditViewModel : StreamViewModelBase, IRoutableViewModel
  {
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = "stream";

    private Client Client { get; }

    #region bindings

    private ConnectorBindings Bindings;

    public ReactiveCommand<Unit, Unit> GoBack => MainWindowViewModel.RouterInstance.NavigateBack;

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
            PreviewImageUrl = _streamState.Client.Account.serverInfo.url + $"/preview/{_streamState.StreamId}";
          else
            PreviewImageUrl = _streamState.Client.Account.serverInfo.url + $"/preview/{_streamState.StreamId}/commits/{_selectedCommit.id}";
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

    #endregion

    private StreamState _streamState { get; }

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

    public StreamEditViewModel()
    {
    }

    public StreamEditViewModel(IScreen screen, StreamState streamState)
    {
      HostScreen = screen;
      Stream = streamState.CachedStream;
      Client = streamState.Client;
      _streamState = streamState; //cached, should not be accessed

      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      //get available filters from our bindings
      AvailableFilters = new List<FilterViewModel>(Bindings.GetSelectionFilters().Select(x => new FilterViewModel(x)));
      SelectedFilter = AvailableFilters[0];

      //get available settings from our bindings
      Settings = Bindings.GetSettings();

      IsReceiver = streamState.IsReceiver;
      GetBranchesAndRestoreState(streamState.Client, streamState);
    }

    private async void GetBranchesAndRestoreState(Client client, StreamState streamState)
    {
      var branches = await client.StreamGetBranches(Stream.id, 100, 0);
      branches.Reverse();
      Branches = branches;

      var branch = Branches.FirstOrDefault(x => x.name == streamState.BranchName);
      if (branch != null)
        SelectedBranch = branch;
      else
        SelectedBranch = Branches[0];

      if (streamState.Filter != null)
      {
        SelectedFilter = AvailableFilters.FirstOrDefault(x => x.Filter.Slug == streamState.Filter.Slug);
        if (SelectedFilter != null)
          SelectedFilter.Filter = streamState.Filter;
      }
      if (streamState.Settings != null)
      {
        foreach (var setting in Settings)
        {
          var savedSetting = streamState.Settings.FirstOrDefault(o => o.Slug == setting.Slug);
          if (savedSetting != null)
            setting.Selection = savedSetting.Selection;
        }
      }
    }

    /// <summary>
    /// The model Stream state, generate it on the fly when needed
    /// </summary>
    private StreamState GetStreamState()
    {
      _streamState.BranchName = SelectedBranch.name;
      _streamState.IsReceiver = IsReceiver;
      if (IsReceiver)
        _streamState.CommitId = SelectedCommit.id;
      if (!IsReceiver)
        _streamState.Filter = SelectedFilter.Filter;
      _streamState.Settings = Settings.Select(o => o).ToList();
      return _streamState;
    }

    private async void GetCommits()
    {
      if (SelectedBranch.commits == null || SelectedBranch.commits.totalCount > 0)
      {
        var branch = await Client.BranchGet(Stream.id, SelectedBranch.name, 100);
        branch.commits.items.Insert(0, new Commit { id = "latest", message = "Always receive the latest commit sent to this branch." });
        Commits = branch.commits.items;
        SelectedCommit = Commits[0];
      }
      else
      {
        SelectedCommit = null;
        Commits = new List<Commit>();
        SelectedCommit = null;
      }
    }

    public void DownloadImage(string url)
    {
      using (WebClient client = new WebClient())
      {
        client.Headers.Set("Authorization", "Bearer " + _streamState.Client.ApiToken);
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

    private void SaveCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      HomeViewModel.Instance.AddSavedStream(GetStreamState());

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

    private async void SendCommand()
    {

      try
      {
        Progress = new ProgressViewModel();
        Progress.IsProgressing = true;
        var dialog = Dialogs.SendReceiveDialog("Sending...", this);

        _ = dialog.ShowDialog(MainWindow.Instance).ContinueWith(x =>
        {
          if (x.Result.GetResult == "cancel")
            Progress.CancellationTokenSource.Cancel();
        }
          );
        await Task.Run(() => Bindings.SendStream(GetStreamState(), Progress));
        dialog.GetWindow().Close();
        Progress.IsProgressing = false;

        if (!Progress.CancellationTokenSource.IsCancellationRequested)
        {
          Analytics.TrackEvent(Client.Account, Analytics.Events.Send, new Dictionary<string, object>() { { "method", "Quick" } });
          Tracker.TrackPageview(Tracker.SEND);
        }

        //TODO: display other dialog if operation failed etc
        MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);

      }
      catch (Exception ex)
      {

      }
    }

    private async void ReceiveCommand()
    {
      try
      {
        Progress = new ProgressViewModel();
        Progress.IsProgressing = true;
        var dialog = Dialogs.SendReceiveDialog("Receiving...", this);

        _ = dialog.ShowDialog(MainWindow.Instance).ContinueWith(x =>
        {
          if (x.Result.GetResult == "cancel")
            Progress.CancellationTokenSource.Cancel();
        });

        await Task.Run(() => Bindings.ReceiveStream(GetStreamState(), Progress));
        dialog.GetWindow().Close();
        Progress.IsProgressing = false;

        if (!Progress.CancellationTokenSource.IsCancellationRequested)
        {
          Analytics.TrackEvent(Client.Account, Analytics.Events.Receive, new Dictionary<string, object>() { { "method", "Quick" } });
          Tracker.TrackPageview(Tracker.RECEIVE);
        }

        //TODO: display other dialog if operation failed etc
        MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      }
      catch (Exception ex)
      {

      }
    }

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

    private void SaveSendCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      HomeViewModel.Instance.AddSavedStream(GetStreamState(), true);
      Analytics.TrackEvent(Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Sender Add" }, { "filter", SelectedFilter.Filter.Name } });
      Tracker.TrackPageview(Tracker.SEND_ADDED);
    }

    private void SaveReceiveCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      HomeViewModel.Instance.AddSavedStream(GetStreamState(), false, true);
      Analytics.TrackEvent(Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Receiver Add" } });
      Tracker.TrackPageview(Tracker.RECEIVE_ADDED);
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
    private bool CanSaveSendCommand(object parameter)
    {
      return IsReady();
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSaveReceiveCommand(object parameter)
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