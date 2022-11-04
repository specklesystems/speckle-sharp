using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows.Dialogs;
using Material.Icons;
using Material.Icons.Avalonia;
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
using System.Threading;
using System.Threading.Tasks;
using Stream = Speckle.Core.Api.Stream;

namespace DesktopUI2.ViewModels
{

  public class HomeViewModel : ReactiveObject, IRoutableViewModel
  {
    //Instance of this HomeViewModel, so that the SavedStreams are kept in memory and not disposed on navigation
    public static HomeViewModel Instance { get; private set; }
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = "home";

    private ConnectorBindings Bindings;

    public enum Filter
    {
      all,
      owner,
      contributor,
      reviewer,
      favorite
    }

    #region bindings
    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string Version => "v" + Bindings.ConnectorVersion;
    public ReactiveCommand<string, Unit> RemoveSavedStreamCommand { get; }

    private CancellationTokenSource StreamGetCancelTokenSource = null;

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

    private ObservableCollection<MenuItemViewModel> _menuItems = new ObservableCollection<MenuItemViewModel>();
    public ObservableCollection<MenuItemViewModel> MenuItems
    {
      get => _menuItems;
      private set
      {
        this.RaiseAndSetIfChanged(ref _menuItems, value);
      }
    }

    private List<StreamAccountWrapper> _streams;
    public List<StreamAccountWrapper> Streams
    {
      get => _streams;
      private set
      {
        this.RaiseAndSetIfChanged(ref _streams, value);
        this.RaisePropertyChanged("FilteredStreams");
        this.RaisePropertyChanged("HasStreams");
      }
    }

    private ObservableCollection<NotificationViewModel> _notifications = new ObservableCollection<NotificationViewModel>();
    public ObservableCollection<NotificationViewModel> Notifications
    {
      get => _notifications;
      private set => this.RaiseAndSetIfChanged(ref _notifications, value);

    }

    private Filter _selectedFilter = Filter.all;
    public Filter SelectedFilter
    {
      get => _selectedFilter;
      private set
      {
        SetFilters(_selectedFilter, value);
      }
    }
    public bool ActiveFilter
    {
      get
      {

        if (SelectedFilter == ViewModels.HomeViewModel.Filter.all)
          return false;
        else
          return true;
      }
    }
    private async void SetFilters(Filter oldValue, Filter newValue)
    {
      this.RaiseAndSetIfChanged(ref _selectedFilter, newValue);
      //refresh stream list if the previous filter is/was favorite
      if (newValue == Filter.favorite || oldValue == Filter.favorite)
      {
        //do not search favourite streams, too much hassle
        if (newValue == Filter.favorite && !string.IsNullOrEmpty(SearchQuery))
          SearchQuery = "";
        await GetStreams();
      }

      this.RaisePropertyChanged("FilteredStreams");
      this.RaisePropertyChanged("HasStreams");
      this.RaisePropertyChanged("ActiveFilter");
    }
    public List<StreamAccountWrapper> FilteredStreams
    {
      get
      {

        if (SelectedFilter == Filter.all || SelectedFilter == Filter.favorite)
          return Streams;
        return Streams.Where(x => x.Stream.role == $"stream:{SelectedFilter}").ToList();
      }
    }

    public bool HasSavedStreams => SavedStreams != null && SavedStreams.Any();
    public bool HasStreams => FilteredStreams != null && FilteredStreams.Any();

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

    private Action streamSearchDebouncer = null;

    private string _searchQuery = "";

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        if (!string.IsNullOrEmpty(SearchQuery) && SearchQuery.Length <= 2)
          return;
        streamSearchDebouncer();
      }
    }


    private StreamViewModel _selectedSavedStream = null;
    private List<StreamViewModel> _savedStreams = new List<StreamViewModel>();
    public List<StreamViewModel> SavedStreams
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
      try
      {
        Instance = this;
        HostScreen = screen;
        RemoveSavedStreamCommand = ReactiveCommand.Create<string>(RemoveSavedStream);

        Bindings = Locator.Current.GetService<ConnectorBindings>();

        Bindings.UpdateSavedStreams = UpdateSavedStreams;
        Bindings.UpdateSelectedStream = UpdateSelectedStream;



        streamSearchDebouncer = Utils.Debounce(SearchStreams, 500);
        Init();
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    /// <summary>
    /// This usually gets triggered on file open or view activated
    /// </summary>
    /// <param name="streams"></param>
    internal void UpdateSavedStreams(List<StreamState> streams)
    {
      try
      {
        SavedStreams.Clear();
        streams.ForEach(x => SavedStreams.Add(new StreamViewModel(x, HostScreen, RemoveSavedStreamCommand)));
        this.RaisePropertyChanged("SavedStreams");
        this.RaisePropertyChanged("HasSavedStreams");


        Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Saved Streams Load" }, { "count", streams.Count } });
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    internal void UpdateSelectedStream()
    {
      try
      {
        if (_selectedSavedStream != null)
          _selectedSavedStream.GetBranchesAndRestoreState();
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }


    internal void WriteStreamsToFile()
    {
      Bindings.WriteStreamsToFile(SavedStreams.Select(x => x.StreamState).ToList());
    }

    internal void AddSavedStream(StreamViewModel stream)
    {
      try
      {
        //saved stream has been edited
        var savedStream = SavedStreams.FirstOrDefault(x => x.StreamState.Id == stream.StreamState.Id);
        if (savedStream != null)
        {
          savedStream = stream;
        }
        //it's a new saved stream
        else
        {
          //triggers => SavedStreams_CollectionChanged
          SavedStreams.Add(stream);

        }

        WriteStreamsToFile();
        this.RaisePropertyChanged("HasSavedStreams");
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    private async Task GetStreams()
    {
      try
      {
        if (!HasAccounts)
          return;

        InProgress = true;

        //needed for the search feature
        StreamGetCancelTokenSource?.Cancel();
        StreamGetCancelTokenSource = new CancellationTokenSource();

        var streams = new List<StreamAccountWrapper>();

        foreach (var account in Accounts)
        {
          if (StreamGetCancelTokenSource.IsCancellationRequested)
            return;

          try
          {
            var client = new Client(account.Account);
            var result = new List<Stream>();

            //NO SEARCH
            if (SearchQuery == "")
            {

              if (SelectedFilter == Filter.favorite)
                result = await client.FavoriteStreamsGet(StreamGetCancelTokenSource.Token, 25);
              else
                result = await client.StreamsGet(StreamGetCancelTokenSource.Token, 25);
            }
            //SEARCH
            else
            {
              //do not search favorite streams, too much hassle
              if (SelectedFilter == Filter.favorite)
                SelectedFilter = Filter.all;
              result = await client.StreamSearch(StreamGetCancelTokenSource.Token, SearchQuery, 25);
            }

            if (StreamGetCancelTokenSource.IsCancellationRequested)
              return;

            streams.AddRange(result.Select(x => new StreamAccountWrapper(x, account.Account)));

          }
          catch (Exception e)
          {
            if (e.InnerException is System.Threading.Tasks.TaskCanceledException)
              return;
            Log.CaptureException(new Exception("Could not fetch streams", e), Sentry.SentryLevel.Error);
            //NOTE: the line below crashes revit at startup! We need to investigate more
            //Dialogs.ShowDialog($"Could not get streams", $"With account {account.Account.userInfo.email} on server {account.Account.serverInfo.url}\n\n" + e.Message, Material.Dialog.Icons.DialogIconKind.Error);
          }
        }
        if (StreamGetCancelTokenSource.IsCancellationRequested)
          return;

        Streams = streams.OrderByDescending(x => DateTime.Parse(x.Stream.updatedAt)).ToList();

        InProgress = false;
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    private async Task GetNotifications()
    {
      try
      {

        var hasUpdate = await Helpers.IsConnectorUpdateAvailable(Bindings.GetHostAppName()).ConfigureAwait(false);

        Notifications.Clear();

        if (hasUpdate)
          Notifications.Add(new NotificationViewModel { Message = "An update for this connector is available, install it now!", Launch = LaunchManagerCommand, Icon = MaterialIconKind.Gift, IconColor = Avalonia.Media.Brushes.Gold });

        foreach (var account in Accounts)
        {
          try
          {
            var client = new Client(account.Account);
            var result = await client.GetAllPendingInvites();
            foreach (var r in result)
            {
              Notifications.Add(new NotificationViewModel(r, client.ServerUrl));
            }

          }
          catch (Exception e)
          {
            if (e.InnerException is System.Threading.Tasks.TaskCanceledException)
              return;
            Log.CaptureException(new Exception("Could not fetch invites", e), Sentry.SentryLevel.Error);
          }
        }


        this.RaisePropertyChanged(nameof(Notifications));

      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    private void SearchStreams()
    {
      GetStreams().ConfigureAwait(false);
      this.RaisePropertyChanged("StreamsText");
    }

    internal async void Init()
    {
      try
      {
        Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();

        GetStreams();
        GetNotifications();
        GenerateMenuItems();

        try
        {
          //first show cached accounts, then refresh them
          await AccountManager.UpdateAccounts();
          Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();
        }
        catch { }



      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    private void GenerateMenuItems()
    {
      try
      {
        MenuItems.Clear();
        MenuItemViewModel menu;


        if (Accounts.Count > 1)
          menu = new MenuItemViewModel { Header = new MaterialIcon { Kind = MaterialIconKind.AccountMultiple, Foreground = Avalonia.Media.Brushes.White } };
        else if (Accounts.Count == 1)
          menu = new MenuItemViewModel { Header = new Image { Width = 28, Height = 28, [!Image.SourceProperty] = new Binding("AvatarImage"), DataContext = Accounts[0], Clip = new EllipseGeometry(new Rect(0, 0, 28, 28)) }, };
        else
          menu = new MenuItemViewModel { Header = new MaterialIcon { Kind = MaterialIconKind.AccountWarning, Foreground = Avalonia.Media.Brushes.White } };

        menu.Items = new List<MenuItemViewModel>();



        foreach (var account in Accounts)
        {

          menu.Items.Add(new MenuItemViewModel
          {
            Header = account.FullAccountName,
            //needs a binding to the image as it's lazy loaded
            Icon = new Image { Width = 20, Height = 20, [!Image.SourceProperty] = new Binding("AvatarImage"), DataContext = account, Clip = new EllipseGeometry(new Rect(0, 0, 20, 20)) },
            Items = new List<MenuItemViewModel>()
            {
              new MenuItemViewModel(OpenProfileCommand, account.Account, "View online", MaterialIconKind.ExternalLink),
              new MenuItemViewModel(RemoveAccountCommand, account.Account, "Remove account", MaterialIconKind.AccountMinus)
            }
          });
        }

        menu.Items.Add(new MenuItemViewModel(AddAccountCommand, "Add another account", MaterialIconKind.AccountPlus));
        menu.Items.Add(new MenuItemViewModel(LaunchManagerCommand, "Manage accounts in Manager", MaterialIconKind.AccountCog));

        menu.Items.Add(new MenuItemViewModel(RefreshCommand, "Refresh streams & accounts", MaterialIconKind.Refresh));
        menu.Items.Add(new MenuItemViewModel(ToggleDarkThemeCommand, "Toggle dark/light theme", MaterialIconKind.SunMoonStars));

#if DEBUG
        menu.Items.Add(new MenuItemViewModel(TestCommand, "Test stuff", MaterialIconKind.Bomb));
#endif

        MenuItems.Add(menu);

        this.RaisePropertyChanged("MenuItems");
      }
      catch (Exception ex)
      {
        new SpeckleException("Error generating menu items", ex, true, Sentry.SentryLevel.Error);
      }
    }

    private void RemoveSavedStream(string id)
    {
      try
      {
        var s = SavedStreams.FirstOrDefault(x => x.StreamState.Id == id);
        if (s != null)
        {
          SavedStreams.Remove(s);
          if (s.StreamState.Client != null)
            Analytics.TrackEvent(s.StreamState.Client.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Remove" } });
        }

        WriteStreamsToFile();
        this.RaisePropertyChanged("HasSavedStreams");
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    public async void RemoveAccountCommand(Account account)
    {
      try
      {
        AccountManager.RemoveAccount(account.id);
        Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account Remove" } });
        Init();
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    public void OpenProfileCommand(Account account)
    {
      Process.Start(new ProcessStartInfo($"{account.serverInfo.url}/profile") { UseShellExecute = true });
      Analytics.TrackEvent(account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account View" } });
    }

    public void LaunchManagerCommand()
    {
      try
      {
        string path = "";

        Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Launch Manager" } });

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
          path = @"/Applications/SpeckleManager.app";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speckle", "Manager", "Manager.exe");
        }

        if (File.Exists(path))
          Process.Start(path);

        else
        {
          Process.Start(new ProcessStartInfo($"https://speckle.systems/download") { UseShellExecute = true });
        }
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }
    public async void AddAccountCommand()
    {
      try
      {
        IsLoggingIn = true;


        var dialog = new AddAccountDialog(AccountManager.GetDefaultServerUrl());
        var result = await dialog.ShowDialog<string>();

        if (result != null)
        {
          Uri u;
          if (!Uri.TryCreate(result, UriKind.Absolute, out u))
            Dialogs.ShowDialog("Error", "Invalid URL", Material.Dialog.Icons.DialogIconKind.Error);
          else
          {
            try
            {
              Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account Add" } });

              await AccountManager.AddAccount(result);
              await Task.Delay(1000);
              Init();
            }
            catch (Exception e)
            {
              Log.CaptureException(e, Sentry.SentryLevel.Error);
              Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
            }
          }
        }

        IsLoggingIn = false;
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }

    public void ClearSearchCommand()
    {
      SearchQuery = "";
    }
    public void ViewOnlineCommand(object parameter)
    {
      var streamAcc = parameter as StreamAccountWrapper;
      Process.Start(new ProcessStartInfo($"{streamAcc.Account.serverInfo.url.TrimEnd('/')}/streams/{streamAcc.Stream.id}") { UseShellExecute = true });
      Analytics.TrackEvent(streamAcc.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream View" } });
    }

    public async void NewStreamCommand()
    {
      var dialog = new NewStreamDialog(Accounts);
      var result = await dialog.ShowDialog<bool>();

      if (result)
      {
        try
        {
          var client = new Client(dialog.Account);
          var streamId = await client.StreamCreate(new StreamCreateInput { description = dialog.Description, name = dialog.StreamName, isPublic = dialog.IsPublic });
          var stream = await client.StreamGet(streamId);
          var streamState = new StreamState(dialog.Account, stream);

          MainViewModel.RouterInstance.Navigate.Execute(new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand));

          Analytics.TrackEvent(dialog.Account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Create" } });

          GetStreams().ConfigureAwait(false); //update streams
        }
        catch (Exception e)
        {
          Log.CaptureException(e, Sentry.SentryLevel.Error);
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

      var result = await dialog.ShowDialog<string>();


      if (result != null)
      {
        try
        {
          var sw = new StreamWrapper(result);
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

          MainViewModel.RouterInstance.Navigate.Execute(new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand));

          Analytics.TrackEvent(account, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Add From URL" } });
        }
        catch (Exception e)
        {
          Log.CaptureException(e, Sentry.SentryLevel.Error);
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



    private void OpenStreamCommand(object streamAccountWrapper)
    {
      if (streamAccountWrapper != null)
      {
        var streamState = new StreamState(streamAccountWrapper as StreamAccountWrapper);
        MainViewModel.RouterInstance.Navigate.Execute(new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand));
        Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Open" } });
      }
    }


    private void OpenSavedStreamCommand(object streamViewModel)
    {
      if (streamViewModel != null && streamViewModel is StreamViewModel svm && !svm.NoAccess)
      {
        try
        {
          svm.UpdateVisualParentAndInit(HostScreen);
          MainViewModel.RouterInstance.Navigate.Execute(svm);
          Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Edit" } });
          _selectedSavedStream = svm;
        }
        catch (Exception ex)
        {
          Log.CaptureException(ex, Sentry.SentryLevel.Error);
        }
      }
    }

    public void ToggleDarkThemeCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Toggle Theme" } });
      var materialTheme = Application.Current.LocateMaterialTheme<MaterialThemeBase>();
      var isDark = materialTheme.CurrentTheme.GetBaseTheme() == BaseThemeMode.Dark;

      MainViewModel.Instance.ChangeTheme(isDark);

      var config = ConfigManager.Load();
      config.DarkTheme = isDark;
      ConfigManager.Save(config);
    }



    public void RefreshCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Refresh" } });
      ApiUtils.ClearCache();
      Init();
    }

    private void OneClickModeCommand()
    {
      MainViewModel.RouterInstance.Navigate.Execute(new OneClickViewModel(HostScreen));

      var config = ConfigManager.Load();
      config.OneClickMode = true;
      ConfigManager.Save(config);
    }

    private void NotificationsCommand()
    {
      MainViewModel.RouterInstance.Navigate.Execute(new NotificationsViewModel(HostScreen, Notifications.ToList()));
    }

    public void TestCommand()
    {
      MainUserControl.NotificationManager.Show(new PopUpNotificationViewModel()
      {
        Title = "🥳 Account removed",
        Message = $"The account has been removed from all your Connectors!",
        Expiration = TimeSpan.Zero,
        Type = Avalonia.Controls.Notifications.NotificationType.Error
      });

      //var dialog = new ImportExportAlert();
      //dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      //dialog.Show();
      //dialog.Activate();
      //dialog.Focus();

    }
  }
}