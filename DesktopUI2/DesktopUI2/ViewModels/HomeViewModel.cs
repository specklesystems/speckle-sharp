using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows.Dialogs;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class HomeViewModel : ReactiveObject, IRoutableViewModel
  {
    //Instance of this HomeViewModel, so that the SavedStreams are kept in memory and not disposed on navigation
    public static HomeViewModel Instance { get; private set; }
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = "home";

    private ConnectorBindings Bindings;

    #region bindings
    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string Version => "v" + Bindings.ConnectorVersion;
    public ReactiveCommand<string, Unit> RemoveSavedStreamCommand { get; }

    private bool _showProgress;
    public bool InProgress
    {
      get => _showProgress;
      private set => this.RaiseAndSetIfChanged(ref _showProgress, value);
    }

    private bool _isLoggingIn;
    public bool IsLoggingIn
    {
      get => _isLoggingIn;
      private set => this.RaiseAndSetIfChanged(ref _isLoggingIn, value);
    }


    private bool _hasUpdate;
    public bool HasUpdate
    {
      get => _hasUpdate;
      private set => this.RaiseAndSetIfChanged(ref _hasUpdate, value);
    }

    private List<StreamAccountWrapper> _streams;
    public List<StreamAccountWrapper> Streams
    {
      get => _streams;
      private set
      {
        this.RaiseAndSetIfChanged(ref _streams, value);
        this.RaisePropertyChanged("HasStreams");
      }
    }

    public bool HasSavedStreams => SavedStreams != null && SavedStreams.Any();
    public bool HasStreams => Streams != null && Streams.Any();

    public string StreamsText
    {
      get
      {
        if (string.IsNullOrEmpty(SearchQuery))
          return "ALL YOUR STREAMS:";

        if (SearchQuery.Length <= 2)
          return "TYPE SOME MORE TO SEARCH...";

        return "SEARCH RESULTS:";

      }
    }

    private string _searchQuery = "";

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        SearchStreams().ConfigureAwait(false);
        this.RaisePropertyChanged("StreamsText");
      }
    }

    public StreamAccountWrapper SelectedStream
    {
      set
      {
        if (value != null)
        {
          var streamState = new StreamState(value);
          OpenStream(streamState);
        }
      }
    }

    public StreamViewModel SelectedSavedStream
    {
      set
      {
        if (value != null && !value.NoAccess)
        {
          value.UpdateHost(HostScreen);
          MainWindowViewModel.RouterInstance.Navigate.Execute(value);
          Tracker.TrackPageview("stream", "edit");
          Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Edit" } });
        }
      }
    }

    private ObservableCollection<StreamViewModel> _savedStreams = new ObservableCollection<StreamViewModel>();
    public ObservableCollection<StreamViewModel> SavedStreams
    {
      get => _savedStreams;
      set
      {
        this.RaiseAndSetIfChanged(ref _savedStreams, value);
        this.RaisePropertyChanged("HasSavedStreams");
      }
    }

    private List<AccountViewModel> _accounts;
    public List<AccountViewModel> Accounts
    {
      get => _accounts;
      private set
      {
        this.RaiseAndSetIfChanged(ref _accounts, value);
        this.RaisePropertyChanged("HasOneAccount");
        this.RaisePropertyChanged("HasMultipleAccounts");
        this.RaisePropertyChanged("HasAccounts");
        this.RaisePropertyChanged("Avatar");
      }
    }

    public Bitmap Avatar
    {
      get => HasAccounts ? Accounts[0].AvatarImage : null;
    }

    public bool HasOneAccount
    {
      get => Accounts.Count == 1;
    }

    public bool HasMultipleAccounts
    {
      get => Accounts.Count > 1;
    }

    public bool HasAccounts
    {
      get => Accounts != null && Accounts.Any();
    }

    #endregion

    public HomeViewModel(IScreen screen)
    {
      Instance = this;
      HostScreen = screen;
      RemoveSavedStreamCommand = ReactiveCommand.Create<string>(RemoveSavedStream);

      SavedStreams.CollectionChanged += SavedStreams_CollectionChanged;

      Bindings = Locator.Current.GetService<ConnectorBindings>();
      this.RaisePropertyChanged("SavedStreams");
      Init();


      var config = ConfigManager.Load();
      ChangeTheme(config.DarkTheme);

    }

    /// <summary>
    /// This get usually triggered on file open or view activated
    /// </summary>
    /// <param name="streams"></param>
    internal void UpdateSavedStreams(List<StreamState> streams)
    {
      SavedStreams.CollectionChanged -= SavedStreams_CollectionChanged;
      SavedStreams = new ObservableCollection<StreamViewModel>();
      streams.ForEach(x => SavedStreams.Add(new StreamViewModel(x, HostScreen, RemoveSavedStreamCommand)));
      this.RaisePropertyChanged("HasSavedStreams");
      SavedStreams.CollectionChanged += SavedStreams_CollectionChanged;
    }

    //write changes to file every time they happen
    //this is because if there is an active document change we need to swap saved streams and restore them later
    //even if the doc has not been saved
    private void SavedStreams_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      WriteStreamsToFile();
    }

    internal void WriteStreamsToFile()
    {
      Bindings.WriteStreamsToFile(SavedStreams.Select(x => x.StreamState).ToList());
    }

    internal void AddSavedStream(StreamViewModel stream)
    {
      //saved stream has been edited
      var savedStream = SavedStreams.FirstOrDefault(x => x.StreamState.Id == stream.StreamState.Id);
      if (savedStream != null)
      {
        savedStream = stream;
        WriteStreamsToFile();
      }
      //it's a new saved stream
      else
      {
        //triggers => SavedStreams_CollectionChanged
        SavedStreams.Add(stream);

      }

      this.RaisePropertyChanged("HasSavedStreams");


    }

    private async Task GetStreams()
    {
      if (!HasAccounts)
        return;

      InProgress = true;

      Streams = new List<StreamAccountWrapper>();

      foreach (var account in Accounts)
      {
        try
        {
          var client = new Client(account.Account);
          Streams.AddRange((await client.StreamsGet()).Select(x => new StreamAccountWrapper(x, account.Account)));
        }
        catch (Exception e)
        {
          Dialogs.ShowDialog($"Could not get streams for {account.Account.userInfo.email} on {account.Account.serverInfo.url}.", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
        }
      }
      Streams = Streams.OrderByDescending(x => DateTime.Parse(x.Stream.updatedAt)).ToList();

      InProgress = false;

    }

    private async Task SearchStreams()
    {
      if (SearchQuery == "")
      {
        GetStreams().ConfigureAwait(false);
        return;
      }
      if (SearchQuery.Length <= 2)
        return;
      InProgress = true;

      Streams = new List<StreamAccountWrapper>();

      foreach (var account in Accounts)
      {
        try
        {
          var client = new Client(account.Account);
          Streams.AddRange((await client.StreamSearch(SearchQuery)).Select(x => new StreamAccountWrapper(x, account.Account)));
        }
        catch (Exception e)
        {

        }
      }

      Streams = Streams.OrderByDescending(x => DateTime.Parse(x.Stream.updatedAt)).ToList();

      InProgress = false;

    }

    internal async void Init()
    {
      Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();

      GetStreams();

      //first show cached accounts, then refresh them
      await AccountManager.UpdateAccounts();
      Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();


      HasUpdate = await Helpers.IsConnectorUpdateAvailable(Bindings.GetHostAppName());
    }

    private void RemoveSavedStream(string id)
    {
      var s = SavedStreams.FirstOrDefault(x => x.StreamState.Id == id);
      if (s != null)
      {
        SavedStreams.Remove(s);
        Tracker.TrackPageview("stream", "remove");
        if (s.StreamState.Client != null)
          Analytics.TrackEvent(s.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Remove" } });
      }

      this.RaisePropertyChanged("HasSavedStreams");
    }


    public async void RemoveAccountCommand(Account account)
    {

      AccountManager.RemoveAccount(account.id);
      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account Remove" } });
      Init();
    }

    public void OpenProfileCommand(Account account)
    {
      Process.Start(new ProcessStartInfo($"{account.serverInfo.url}/profile") { UseShellExecute = true });
      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account View" } });
    }

    public void LaunchManagerCommand()
    {
      string path = "";

      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Launch Manager" } });

      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        path = @"/Applications/SpeckleManager.app";
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "speckle-manager", "SpeckleManager.exe");
      }

      if (File.Exists(path))
        Process.Start(path);

      else
      {
        Process.Start(new ProcessStartInfo($"https://speckle-releases.netlify.app/") { UseShellExecute = true });
      }

    }
    public async void AddAccountCommand()
    {
      IsLoggingIn = true;


      var dialog = new AddAccountDialog(AccountManager.GetDefaultServerUrl());
      dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      await dialog.ShowDialog(MainWindow.Instance);

      if (dialog.Add)
      {
        Uri u;
        if (!Uri.TryCreate(dialog.Url, UriKind.Absolute, out u))
          Dialogs.ShowDialog("Error", "Invalid URL", Material.Dialog.Icons.DialogIconKind.Error);
        else
        {
          try
          {
            Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account Add" } });

            await AccountManager.AddAccount(dialog.Url);
            await Task.Delay(1000);
            Init();
          }
          catch (Exception e)
          {
            Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
          }
        }
      }

      IsLoggingIn = false;

    }

    public void ClearSearchCommand()
    {
      SearchQuery = "";
    }
    public void ViewOnlineCommand(object parameter)
    {
      var streamAcc = parameter as StreamAccountWrapper;
      Process.Start(new ProcessStartInfo($"{streamAcc.Account.serverInfo.url.TrimEnd('/')}/streams/{streamAcc.Stream.id}") { UseShellExecute = true });
      Tracker.TrackPageview(Tracker.STREAM_VIEW);
      Analytics.TrackEvent(streamAcc.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream View" } });
    }

    public void SendCommand(object parameter)
    {
      var streamAcc = parameter as StreamAccountWrapper;
      var streamState = new StreamState(streamAcc);
      OpenStream(streamState);
    }

    public void ReceiveCommand(object parameter)
    {
      var streamAcc = parameter as StreamAccountWrapper;
      var streamState = new StreamState(streamAcc) { IsReceiver = true };
      OpenStream(streamState);
    }

    public async void NewStreamCommand()
    {
      var dialog = new NewStreamDialog(Accounts);
      dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      await dialog.ShowDialog(MainWindow.Instance);



      if (dialog.Create)
      {
        try
        {
          var client = new Client(dialog.Account);
          var streamId = await client.StreamCreate(new StreamCreateInput { description = dialog.Description, name = dialog.StreamName, isPublic = dialog.IsPublic });
          var stream = await client.StreamGet(streamId);
          var streamState = new StreamState(dialog.Account, stream);

          OpenStream(streamState);

          Tracker.TrackPageview(Tracker.STREAM_CREATE);
          Analytics.TrackEvent(dialog.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Create" } });

          GetStreams().ConfigureAwait(false); //update streams
        }
        catch (Exception e)
        {
          Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
        }
      }
    }

    [DependsOn(nameof(InProgress))]
    public bool CanNewStreamCommand(object parameter)
    {
      return !InProgress;
    }

    public async void AddFromUrlCommand()
    {
      var clipboard = await Avalonia.Application.Current.Clipboard.GetTextAsync();

      Uri uri;
      string defaultText = "";
      if (Uri.TryCreate(clipboard, UriKind.Absolute, out uri))
        defaultText = clipboard;

      var dialog = new AddFromUrlDialog(defaultText);
      dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      await dialog.ShowDialog(MainWindow.Instance);


      if (dialog.Add)
      {
        try
        {
          var sw = new StreamWrapper(dialog.Url);
          var account = await sw.GetAccount();
          var client = new Client(account);
          var stream = await client.StreamGet(sw.StreamId);
          var streamState = new StreamState(account, stream);
          streamState.BranchName = sw.BranchName;

          //it's a commit URL, let's pull the right branch name
          if (sw.CommitId != null)
          {
            streamState.IsReceiver = true;
            streamState.CommitId = sw.CommitId;

            var commit = await client.CommitGet(sw.StreamId, sw.CommitId);
            streamState.BranchName = commit.branchName;
          }

          OpenStream(streamState);

          Tracker.TrackPageview("stream", "add-from-url");
          Analytics.TrackEvent(account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Add From URL" } });
        }
        catch (Exception e)
        {
          Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
        }
      }
    }

    private Tuple<bool, string> ValidateUrl(string url)
    {
      Uri uri;
      try
      {
        if (Uri.TryCreate(url, UriKind.Absolute, out uri))
        {
          var sw = new StreamWrapper(url);
        }
        else return new Tuple<bool, string>(false, "URL is not valid.");
      }
      catch { return new Tuple<bool, string>(false, "URL is not a Stream."); }

      return new Tuple<bool, string>(true, "");
    }

    private Tuple<bool, string> ValidateName(string name)
    {
      if (string.IsNullOrEmpty(name))
        return new Tuple<bool, string>(false, "Streams need a name too!");

      if (name.Trim().Length < 3)
        return new Tuple<bool, string>(false, "Name is too short.");

      return new Tuple<bool, string>(true, "");
    }

    [DependsOn(nameof(InProgress))]
    public bool CanAddFromUrlCommand(object parameter)
    {
      return !InProgress;
    }

    private void OpenStream(StreamState streamState)
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand));
    }

    public void ToggleDarkThemeCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Toggle Theme" } });
      var paletteHelper = new PaletteHelper();
      ITheme theme = paletteHelper.GetTheme();
      var isDark = theme.GetBaseTheme() == BaseThemeMode.Dark;

      ChangeTheme(isDark);

      var config = ConfigManager.Load();
      config.DarkTheme = isDark;
      ConfigManager.Save(config);

    }

    private void ChangeTheme(bool isDark)
    {
      var paletteHelper = new PaletteHelper();
      var theme = paletteHelper.GetTheme();

      if (isDark)
        theme.SetBaseTheme(BaseThemeMode.Light.GetBaseTheme());
      else
        theme.SetBaseTheme(BaseThemeMode.Dark.GetBaseTheme());
      paletteHelper.SetTheme(theme);
    }


    public void RefreshCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Refresh" } });
      Init();
    }


  }
}