using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using DesktopUI2.Models;
using DesktopUI2.Models.TypeMappingOnReceive;
using DesktopUI2.Views;
using DesktopUI2.Views.Windows.Dialogs;
using Material.Dialog.Icons;
using Material.Icons;
using Material.Icons.Avalonia;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using ReactiveUI;
using Sentry.Extensibility;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Splat;

namespace DesktopUI2.ViewModels;

public class HomeViewModel : ReactiveObject, IRoutableViewModel
{
  public enum Filter
  {
    all,
    owner,
    contributor,
    reviewer,
    favorite
  }

  private ConnectorBindings Bindings;

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
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(
        ex,
        "Failed to construct view model {viewModel} {exceptionMessage}",
        GetType(),
        ex.Message
      );
    }
  }

  //Instance of this HomeViewModel, so that the SavedStreams are kept in memory and not disposed on navigation
  public static HomeViewModel Instance { get; private set; }
  public IScreen HostScreen { get; }

  public string UrlPathSegment { get; } = "home";

  /// <summary>
  /// This usually gets triggered on file open or view activated
  /// </summary>
  /// <param name="streams"></param>
  internal void UpdateSavedStreams(List<StreamState> streams)
  {
    try
    {
      ClearSavedStreams();

      foreach (StreamState stream in streams)
        SavedStreams.Add(new StreamViewModel(stream, HostScreen, RemoveSavedStreamCommand));

      this.RaisePropertyChanged(nameof(SavedStreams));
      this.RaisePropertyChanged(nameof(HasSavedStreams));

      //Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Saved Streams Load" }, { "count", streams.Count } }, isAction: false);
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Could not Update Saved Streams {exceptionMessage}", ex.Message);
    }
  }

  private void ClearSavedStreams()
  {
    //dispose subscriptions!
    SavedStreams.ForEach(x => x.Dispose());
    SavedStreams.Clear();
  }

  /// <summary>
  /// Binding from host app when the saved stream needs to refresh filters & such
  /// </summary>
  internal void UpdateSelectedStream()
  {
    try
    {
      if (_selectedSavedStream != null && !_selectedSavedStream.Progress.IsProgressing)
        _selectedSavedStream.GetBranchesAndRestoreState();
    }
    catch (Exception ex)
    {
      //FIXME: This branch can't ever get hit right?
      SpeckleLog.Logger.Error(ex, "Failed updating selected stream {exceptionMessage}", ex.Message);
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
        savedStream = stream;
      //it's a new saved stream
      else
        SavedStreams.Add(stream);

      WriteStreamsToFile();
      this.RaisePropertyChanged(nameof(HasSavedStreams));
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed to add saved stream {exceptionMessage}", ex.Message);
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
          var result = new List<Stream>();

          //NO SEARCH
          if (string.IsNullOrEmpty(SearchQuery))
          {
            if (SelectedFilter == Filter.favorite)
              result = await account.Client
                .FavoriteStreamsGet(StreamGetCancelTokenSource.Token, 25)
                .ConfigureAwait(true);
            else
              result = await account.Client.StreamsGet(StreamGetCancelTokenSource.Token, 25).ConfigureAwait(true);
          }
          //SEARCH
          else
          {
            //do not search favorite streams, too much hassle
            if (SelectedFilter == Filter.favorite)
              SelectedFilter = Filter.all;
            result = await account.Client
              .StreamSearch(StreamGetCancelTokenSource.Token, SearchQuery, 25)
              .ConfigureAwait(true);
          }

          if (StreamGetCancelTokenSource.IsCancellationRequested)
            return;

          streams.AddRange(result.Select(x => new StreamAccountWrapper(x, account.Account)));
        }
        catch (OperationCanceledException)
        {
          continue;
        }
        catch (Exception ex)
        {
          if (ex.InnerException is TaskCanceledException)
            return;

          SpeckleLog.Logger.Error(ex, "Could not fetch streams");

          Dispatcher.UIThread.Post(
            () =>
              MainUserControl.NotificationManager.Show(
                new PopUpNotificationViewModel
                {
                  Title = "⚠️ Could not get streams",
                  Message =
                    $"With account {account.Account.userInfo.email} on server {account.Account.serverInfo.url}\n\n",
                  Type = NotificationType.Error
                }
              ),
            DispatcherPriority.Background
          );
        }
      }
      if (StreamGetCancelTokenSource.IsCancellationRequested)
        return;

      Streams = streams.OrderByDescending(x => x.Stream.updatedAt).ToList();
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(ex, "Unexpected exception while getting streams {exceptionMessage}", ex.Message);
    }
    finally
    {
      InProgress = false;
    }
  }

  private async Task GetNotifications()
  {
    try
    {
      var hasUpdate = await Helpers.IsConnectorUpdateAvailable(Bindings.GetHostAppName()).ConfigureAwait(true);

      Notifications.Clear();

      if (hasUpdate)
      {
        Notifications.Add(
          new NotificationViewModel
          {
            Message = "An update for this connector is available, install it now!",
            Launch = LaunchManagerCommand,
            Icon = MaterialIconKind.Gift,
            IconColor = Brushes.Gold
          }
        );
      }

      foreach (var account in Accounts)
      {
        try
        {
          var result = await account.Client.GetAllPendingInvites().ConfigureAwait(true);
          foreach (var r in result)
            Notifications.Add(new NotificationViewModel(r, account.Client.ServerUrl));
        }
        catch (Exception e)
        {
          if (e.InnerException is TaskCanceledException)
            return;

          SpeckleLog.Logger.Error(e, "Could not fetch invites");
        }
      }

      this.RaisePropertyChanged(nameof(Notifications));
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(
        ex,
        "Swallowing exception in {methodName}: {exceptionMessage}",
        nameof(GetNotifications),
        ex.Message
      );
    }
  }

  private async void SearchStreams()
  {
    if (await CheckIsOffline().ConfigureAwait(true))
      return;

    GetStreams().ConfigureAwait(true);
    this.RaisePropertyChanged(nameof(StreamsText));
  }

  private async Task<bool> CheckIsOffline()
  {
    if (!await Http.UserHasInternet().ConfigureAwait(true))
    {
      Dispatcher.UIThread.Post(
        () =>
          MainUserControl.NotificationManager.Show(
            new PopUpNotificationViewModel
            {
              Title = "⚠️ Oh no!",
              Message = "Could not reach the internet, are you connected?",
              Type = NotificationType.Error
            }
          ),
        DispatcherPriority.Background
      );

      IsOffline = true;
    }
    else
    {
      IsOffline = false;
    }

    return IsOffline;
  }

  internal async void Refresh()
  {
    try
    {
      if (await CheckIsOffline().ConfigureAwait(true))
        return;

      //prevent subscriptions from being registered multiple times
      //DISABLED: https://github.com/specklesystems/speckle-sharp/issues/2574
      //_subscribedClientsStreamAddRemove.ForEach(x => x.Dispose());
      //_subscribedClientsStreamAddRemove.Clear();

      Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();

      GetStreams();
      GetNotifications();
      GenerateMenuItems();

      try
      {
        //first show cached accounts, then refresh them
        await AccountManager.UpdateAccounts().ConfigureAwait(true);
        Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Warning(
          ex,
          "Swallowing exception in {methodName}: {exceptionMessage}",
          nameof(Refresh),
          ex.Message
        );
      }

      //DISABLED: https://github.com/specklesystems/speckle-sharp/issues/2574
      //foreach (var account in Accounts)
      //{
      //  account.Client.SubscribeUserStreamAdded();
      //  account.Client.OnUserStreamAdded += Client_OnUserStreamAdded;

      //  account.Client.SubscribeUserStreamRemoved();
      //  account.Client.OnUserStreamRemoved += Client_OnUserStreamRemoved;

      //  _subscribedClientsStreamAddRemove.Add(account.Client);
      //}
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed to refresh {exceptionMessage}", ex.Message);
    }
  }

  private void Client_OnUserStreamAdded(object sender, StreamInfo e)
  {
    Dispatcher.UIThread.Post(() =>
    {
      MainUserControl.NotificationManager.Show(
        new PopUpNotificationViewModel
        {
          Title = "🥳 You have a new Stream!",
          Message = e.sharedBy == null ? $"You have created '{e.name}'." : $"'{e.name}' has been shared with you."
        }
      );
      ;
    });
  }

  private void Client_OnUserStreamRemoved(object sender, StreamInfo e)
  {
    Dispatcher.UIThread.Post(() =>
    {
      var streamName = Streams.FirstOrDefault(x => x.Stream.id == e.id)?.Stream?.name;
      if (streamName == null)
        streamName = SavedStreams.FirstOrDefault(x => x.Stream.id == e.id)?.Stream?.name;
      if (streamName == null)
        return;

      var svm = MainViewModel.RouterInstance.NavigationStack.Last() as StreamViewModel;
      if (svm != null && svm.Stream.id == e.id)
        MainViewModel.GoHome();

      //remove all saved streams matching this id
      foreach (var stateId in SavedStreams.Where(x => x.Stream.id == e.id).Select(y => y.StreamState.Id).ToList())
        RemoveSavedStream(stateId);

      GetStreams().ConfigureAwait(true);

      MainUserControl.NotificationManager.Show(
        new PopUpNotificationViewModel
        {
          Title = "❌ Stream removed!",
          Message = $"'{streamName}' has been deleted or un-shared."
        }
      );
      ;
    });
  }

  private void GenerateMenuItems()
  {
    try
    {
      MenuItems.Clear();
      MenuItemViewModel menu;

      if (Accounts.Count > 1)
        menu = new MenuItemViewModel
        {
          Header = new MaterialIcon { Kind = MaterialIconKind.AccountMultiple, Foreground = Brushes.White }
        };
      else if (Accounts.Count == 1)
        menu = new MenuItemViewModel
        {
          Header = new Image
          {
            Width = 28,
            Height = 28,
            [!Image.SourceProperty] = new Binding("AvatarImage"),
            DataContext = Accounts[0],
            Clip = new EllipseGeometry(new Rect(0, 0, 28, 28))
          }
        };
      else
        menu = new MenuItemViewModel
        {
          Header = new MaterialIcon { Kind = MaterialIconKind.AccountWarning, Foreground = Brushes.White }
        };

      menu.Items = new List<MenuItemViewModel>();

      foreach (var account in Accounts)
        menu.Items.Add(
          new MenuItemViewModel
          {
            Header = account.FullAccountName,
            //needs a binding to the image as it's lazy loaded
            Icon = new Image
            {
              Width = 20,
              Height = 20,
              [!Image.SourceProperty] = new Binding("AvatarImage"),
              DataContext = account,
              Clip = new EllipseGeometry(new Rect(0, 0, 20, 20))
            },
            Items = new List<MenuItemViewModel>
            {
              new(OpenProfileCommand, account.Account, "View online", MaterialIconKind.ExternalLink),
              new(RemoveAccountCommand, account.Account, "Remove account", MaterialIconKind.AccountMinus)
            }
          }
        );

      menu.Items.Add(new MenuItemViewModel(AddAccountCommand, "Add another account", MaterialIconKind.AccountPlus));
      menu.Items.Add(
        new MenuItemViewModel(LaunchManagerCommand, "Manage accounts in Manager", MaterialIconKind.AccountCog)
      );

      menu.Items.Add(new MenuItemViewModel(RefreshCommand, "Refresh streams & accounts", MaterialIconKind.Refresh));
      menu.Items.Add(
        new MenuItemViewModel(ToggleDarkThemeCommand, "Toggle dark/light theme", MaterialIconKind.SunMoonStars)
      );

      menu.Items.Add(new MenuItemViewModel(ToggleFe2Command, "Toggle NEW Frontend support", MaterialIconKind.NewBox));

#if DEBUG
      menu.Items.Add(new MenuItemViewModel(TestCommand, "Test stuff", MaterialIconKind.Bomb));
#endif

      MenuItems.Add(menu);

      this.RaisePropertyChanged(nameof(MenuItems));
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Error generating menu items {exceptionMessage}", ex.Message);
    }
  }

  private void RemoveSavedStream(string stateId)
  {
    try
    {
      var i = SavedStreams.FindIndex(x => x.StreamState.Id == stateId);
      if (i == -1)
        return;
      SavedStreams[i].Dispose();
      SavedStreams.RemoveAt(i);

      SavedStreams = SavedStreams.ToList();

      WriteStreamsToFile();

      this.RaisePropertyChanged(nameof(SavedStreams));
      this.RaisePropertyChanged(nameof(HasSavedStreams));

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Stream Remove" } });
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed to remove saved stream {exceptionMessage}", ex.Message);
    }
  }

  public async void AddAccountCommand()
  {
    InProgress = true;
    await Utils.AddAccountCommand().ConfigureAwait(true);
    InProgress = false;
  }

  public async void RemoveAccountCommand(Account account)
  {
    try
    {
      AccountManager.RemoveAccount(account.id);
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Account Remove" } });
      MainViewModel.Instance.NavigateToDefaultScreen();
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Failed to remove account {exceptionMessage}", ex.Message);
    }
  }

  public void OpenProfileCommand(Account account)
  {
    Process.Start(new ProcessStartInfo($"{account.serverInfo.url}/profile") { UseShellExecute = true });
    Analytics.TrackEvent(
      account,
      Analytics.Events.DUIAction,
      new Dictionary<string, object> { { "name", "Account View" } }
    );
  }

  public void LaunchManagerCommand()
  {
    Utils.LaunchManager();
  }

  public void ClearSearchCommand()
  {
    SearchQuery = "";
  }

  public void ViewOnlineCommand(object parameter)
  {
    var streamAcc = parameter as StreamAccountWrapper;
    var url = $"{streamAcc.Account.serverInfo.url.TrimEnd('/')}/streams/{streamAcc.Stream.id}";

    if (UseFe2)
      url = $"{streamAcc.Account.serverInfo.url.TrimEnd('/')}/projects/{streamAcc.Stream.id}";

    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    Analytics.TrackEvent(
      streamAcc.Account,
      Analytics.Events.DUIAction,
      new Dictionary<string, object> { { "name", "Stream View" } }
    );
  }

  public async void NewStreamCommand()
  {
    var dialog = new NewStreamDialog(Accounts);
    var result = await dialog.ShowDialog<bool>().ConfigureAwait(true);

    if (result)
      try
      {
        using var client = new Client(dialog.Account);
        var streamId = await client
          .StreamCreate(
            new StreamCreateInput
            {
              description = dialog.Description,
              name = dialog.StreamName,
              isPublic = dialog.IsPublic
            }
          )
          .ConfigureAwait(true);
        var stream = await client.StreamGet(streamId).ConfigureAwait(true);
        var streamState = new StreamState(dialog.Account, stream);

        MainViewModel.RouterInstance.Navigate.Execute(
          new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand)
        );

        Analytics.TrackEvent(
          dialog.Account,
          Analytics.Events.DUIAction,
          new Dictionary<string, object> { { "name", "Stream Create" } }
        );

        GetStreams().ConfigureAwait(true); //update streams
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Fatal(ex, "Failed to create new stream {exceptionMessage}", ex.Message);
        Dialogs.ShowDialog("Something went wrong...", ex.Message, DialogIconKind.Error);
      }
  }

  [DependsOn(nameof(InProgress))]
  public bool CanNewStreamCommand(object parameter)
  {
    return !InProgress;
  }

  public async void AddFromUrlCommand()
  {
    var clipboard = await Application.Current.Clipboard.GetTextAsync().ConfigureAwait(true);

    Uri uri;
    string defaultText = "";
    if (Uri.TryCreate(clipboard, UriKind.Absolute, out uri))
      defaultText = clipboard;

    var dialog = new AddFromUrlDialog(defaultText);

    var result = await dialog.ShowDialog<string>().ConfigureAwait(true);

    if (result != null)
      try
      {
        var sw = new StreamWrapper(result);
        var account = await sw.GetAccount().ConfigureAwait(true);
        using var client = new Client(account);
        var stream = await client.StreamGet(sw.StreamId).ConfigureAwait(true);
        var streamState = new StreamState(account, stream);
        streamState.BranchName = sw.BranchName;

        //it's a commit URL, let's pull the right branch name
        if (sw.CommitId != null)
        {
          streamState.IsReceiver = true;
          streamState.CommitId = sw.CommitId;

          var commit = await client.CommitGet(sw.StreamId, sw.CommitId).ConfigureAwait(true);
          streamState.BranchName = commit.branchName;
        }

        MainViewModel.RouterInstance.Navigate.Execute(
          new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand)
        );

        Analytics.TrackEvent(
          account,
          Analytics.Events.DUIAction,
          new Dictionary<string, object> { { "name", "Stream Add From URL" } }
        );
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Fatal(ex, "Failed to add from url {dialogResult} {exceptionMessage}", result, ex.Message);
        Dialogs.ShowDialog("Something went wrong...", ex.Message, DialogIconKind.Error);
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
      else
      {
        return new Tuple<bool, string>(false, "URL is not valid.");
      }
    }
    catch
    {
      return new Tuple<bool, string>(false, "URL is not a Stream.");
    }

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

  private async void OpenStreamCommand(object streamAccountWrapper)
  {
    if (await CheckIsOffline().ConfigureAwait(true))
      return;

    if (streamAccountWrapper != null)
    {
      var streamState = new StreamState(streamAccountWrapper as StreamAccountWrapper);

      if (!await streamState.Client.IsStreamAccessible(streamState.StreamId).ConfigureAwait(true))
      {
        Dialogs.ShowDialog(
          "Stream not found",
          "Please ensure the stream exists and that you have access to it.",
          DialogIconKind.Error
        );
        return;
      }

      MainViewModel.RouterInstance.Navigate.Execute(
        new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand)
      );
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Stream Open" } });
    }
  }

  private async void OpenSavedStreamCommand(object streamViewModel)
  {
    if (await CheckIsOffline().ConfigureAwait(true))
      return;

    if (streamViewModel != null && streamViewModel is StreamViewModel svm && !svm.NoAccess)
    {
      try
      {
        if (!await svm.Client.IsStreamAccessible(svm.Stream.id).ConfigureAwait(true))
        {
          Dialogs.ShowDialog(
            "Stream not found",
            "Please ensure the stream exists and that you have access to it.",
            DialogIconKind.Error
          );
          return;
        }

        svm.UpdateVisualParentAndInit(HostScreen);
        MainViewModel.RouterInstance.Navigate.Execute(svm);
        Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Stream Edit" } });
        _selectedSavedStream = svm;
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Failed to open saved stream {exceptionMessage}", ex.Message);
      }
    }
  }

  public void ToggleDarkThemeCommand()
  {
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Toggle Theme" } });
    var materialTheme = Application.Current.LocateMaterialTheme<MaterialThemeBase>();
    var isDark = materialTheme.CurrentTheme.GetBaseTheme() == BaseThemeMode.Dark;

    MainViewModel.Instance.ChangeTheme(isDark);

    var config = ConfigManager.Load();
    config.DarkTheme = isDark;
    ConfigManager.Save(config);
  }

  public void ToggleFe2Command()
  {
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Toggle Fe2" } });

    var config = ConfigManager.Load();
    config.UseFe2 = !config.UseFe2;
    ConfigManager.Save(config);

    this.RaisePropertyChanged(nameof(UseFe2));
  }

  public void RefreshCommand()
  {
    Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object> { { "name", "Refresh" } });
    ApiUtils.ClearCache();
    Refresh();
  }

  private void OneClickModeCommand()
  {
    var config = ConfigManager.Load();
    config.OneClickMode = true;
    ConfigManager.Save(config);

    MainViewModel.Instance.NavigateToDefaultScreen();
  }

  private void NotificationsCommand()
  {
    MainViewModel.RouterInstance.Navigate.Execute(new NotificationsViewModel(HostScreen, Notifications.ToList()));
  }

  public void TestCommand()
  {
    MainUserControl.NotificationManager.Show(
      new PopUpNotificationViewModel
      {
        Title = "🥳 Account removed",
        Message = "The account has been removed from all your Connectors!",
        Expiration = TimeSpan.Zero,
        Type = NotificationType.Error
      }
    );

    //var dialog = new ImportExportAlert();
    //dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    //dialog.Show();
    //dialog.Activate();
    //dialog.Focus();
  }

  #region bindings

  public string Title => "for " + Bindings.GetHostAppNameVersion();
  public string Version => "v" + Bindings.ConnectorVersion;
  public ReactiveCommand<string, Unit> RemoveSavedStreamCommand { get; }

  private CancellationTokenSource StreamGetCancelTokenSource;

  private bool _showProgress;

  public bool InProgress
  {
    get => _showProgress;
    private set => this.RaiseAndSetIfChanged(ref _showProgress, value);
  }

  private ObservableCollection<MenuItemViewModel> _menuItems = new();

  public ObservableCollection<MenuItemViewModel> MenuItems
  {
    get => _menuItems;
    private set => this.RaiseAndSetIfChanged(ref _menuItems, value);
  }

  private List<StreamAccountWrapper> _streams;

  public List<StreamAccountWrapper> Streams
  {
    get => _streams;
    private set
    {
      this.RaiseAndSetIfChanged(ref _streams, value);
      this.RaisePropertyChanged(nameof(FilteredStreams));
      this.RaisePropertyChanged(nameof(HasStreams));
    }
  }

  private ObservableCollection<NotificationViewModel> _notifications = new();

  public ObservableCollection<NotificationViewModel> Notifications
  {
    get => _notifications;
    private set => this.RaiseAndSetIfChanged(ref _notifications, value);
  }

  private Filter _selectedFilter = Filter.all;

  public Filter SelectedFilter
  {
    get => _selectedFilter;
    private set => SetFilters(_selectedFilter, value);
  }

  public bool ActiveFilter
  {
    get
    {
      if (SelectedFilter == Filter.all)
        return false;
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
      await GetStreams().ConfigureAwait(true);
    }

    this.RaisePropertyChanged(nameof(FilteredStreams));
    this.RaisePropertyChanged(nameof(HasStreams));
    this.RaisePropertyChanged(nameof(ActiveFilter));
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

  public bool UseFe2
  {
    get
    {
      var config = ConfigManager.Load();
      return config.UseFe2;
    }
  }

  public string StreamsText
  {
    get
    {
      if (string.IsNullOrEmpty(SearchQuery))
      {
        if (UseFe2)
          return "ALL YOUR PROJECTS:";
        return "ALL YOUR STREAMS:";
      }

      if (SearchQuery.Length <= 2)
        return "TYPE SOME MORE TO SEARCH...";

      return "SEARCH RESULTS:";
    }
  }

  private Action streamSearchDebouncer;

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

  private StreamViewModel _selectedSavedStream;
  private List<StreamViewModel> _savedStreams = new();

  public List<StreamViewModel> SavedStreams
  {
    get => _savedStreams;
    set
    {
      this.RaiseAndSetIfChanged(ref _savedStreams, value);
      this.RaisePropertyChanged(nameof(HasSavedStreams));
    }
  }

  private List<AccountViewModel> _accounts;

  public List<AccountViewModel> Accounts
  {
    get => _accounts;
    private set
    {
      this.RaiseAndSetIfChanged(ref _accounts, value);
      this.RaisePropertyChanged(nameof(HasAccounts));
      this.RaisePropertyChanged("Avatar");
    }
  }

  public bool HasAccounts => Accounts != null && Accounts.Any();

  private List<Client> _subscribedClientsStreamAddRemove = new();

  private bool _isOffline;

  public bool IsOffline
  {
    get => _isOffline;
    private set => this.RaiseAndSetIfChanged(ref _isOffline, value);
  }

  #endregion
}
