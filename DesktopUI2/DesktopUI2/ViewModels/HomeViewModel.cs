using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using DesktopUI2.Models;
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
        this.RaisePropertyChanged("FilteredStreams");
        this.RaisePropertyChanged("HasStreams");
      }
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

    private string _searchQuery = "";

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        GetStreams().ConfigureAwait(false);
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

    private StreamViewModel _selectedSavedStream;
    public StreamViewModel SelectedSavedStream
    {
      set
      {
        if (value != null && !value.NoAccess)
        {
          try
          {
            value.UpdateVisualParentAndInit(HostScreen);
            MainViewModel.RouterInstance.Navigate.Execute(value);
            Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Edit" } });
            _selectedSavedStream = value;
          }
          catch (Exception ex)
          {

          }
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
      try
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
      catch (Exception ex)
      {

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
        SavedStreams.CollectionChanged -= SavedStreams_CollectionChanged;
        SavedStreams = new ObservableCollection<StreamViewModel>();
        streams.ForEach(x => SavedStreams.Add(new StreamViewModel(x, HostScreen, RemoveSavedStreamCommand)));
        this.RaisePropertyChanged("HasSavedStreams");
        SavedStreams.CollectionChanged += SavedStreams_CollectionChanged;
      }
      catch (Exception ex)
      {

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

      }
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
      try
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
      catch (Exception ex)
      {

      }
    }

    private async Task GetStreams()
    {
      try
      {
        if (!HasAccounts || (!string.IsNullOrEmpty(SearchQuery) && SearchQuery.Length <= 2))
          return;

        InProgress = true;

        Streams = new List<StreamAccountWrapper>();

        foreach (var account in Accounts)
        {
          try
          {
            var client = new Client(account.Account);

            //NO SEARCH
            if (SearchQuery == "")
            {

              if (SelectedFilter == Filter.favorite)
                Streams.AddRange((await client.FavoriteStreamsGet()).Select(x => new StreamAccountWrapper(x, account.Account)));
              else
                Streams.AddRange((await client.StreamsGet()).Select(x => new StreamAccountWrapper(x, account.Account)));
            }
            //SEARCH
            else
            {
              //do not search favorite streams, too much hassle
              if (SelectedFilter == Filter.favorite)
                SelectedFilter = Filter.all;
              Streams.AddRange((await client.StreamSearch(SearchQuery)).Select(x => new StreamAccountWrapper(x, account.Account)));
            }

          }
          catch (Exception e)
          {
            Dialogs.ShowDialog($"Could not get streams", $"With account {account.Account.userInfo.email} on server {account.Account.serverInfo.url}\n\n" + e.Message, Material.Dialog.Icons.DialogIconKind.Error);
          }
        }
        Streams = Streams.OrderByDescending(x => DateTime.Parse(x.Stream.updatedAt)).ToList();

        InProgress = false;
      }
      catch (Exception ex)
      {

      }

    }



    internal async void Init()
    {
      try
      {
        Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();

        GetStreams();

        try
        {
          //first show cached accounts, then refresh them
          await AccountManager.UpdateAccounts();
          Accounts = AccountManager.GetAccounts().Select(x => new AccountViewModel(x)).ToList();
        }
        catch { }


        HasUpdate = await Helpers.IsConnectorUpdateAvailable(Bindings.GetHostAppName());
      }
      catch (Exception ex)
      {

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

        this.RaisePropertyChanged("HasSavedStreams");
      }
      catch (Exception ex)
      {

      }
    }


    public async void RemoveAccountCommand(Account account)
    {
      try
      {
        AccountManager.RemoveAccount(account.id);
        Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account Remove" } });
        Init();
      }
      catch (Exception ex)
      {

      }
    }

    public void OpenProfileCommand(Account account)
    {
      Process.Start(new ProcessStartInfo($"{account.serverInfo.url}/profile") { UseShellExecute = true });
      Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account View" } });
    }

    public void LaunchManagerCommand()
    {
      try
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
      catch (Exception ex)
      {

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
              Analytics.TrackEvent(null, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account Add" } });

              await AccountManager.AddAccount(result);
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
      catch (Exception ex)
      {

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
      var result = await dialog.ShowDialog<bool>();



      if (result)
      {
        try
        {
          var client = new Client(dialog.Account);
          var streamId = await client.StreamCreate(new StreamCreateInput { description = dialog.Description, name = dialog.StreamName, isPublic = dialog.IsPublic });
          var stream = await client.StreamGet(streamId);
          var streamState = new StreamState(dialog.Account, stream);

          OpenStream(streamState);

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

          OpenStream(streamState);

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
      MainViewModel.RouterInstance.Navigate.Execute(new StreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand));
    }

    public void ToggleDarkThemeCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Toggle Theme" } });
      var materialTheme = Application.Current.LocateMaterialTheme<MaterialThemeBase>();
      var isDark = materialTheme.CurrentTheme.GetBaseTheme() == BaseThemeMode.Dark;

      ChangeTheme(isDark);

      var config = ConfigManager.Load();
      config.DarkTheme = isDark;
      ConfigManager.Save(config);

    }

    private void ChangeTheme(bool isDark)
    {

      if (Application.Current == null)
        return;

      var materialTheme = Application.Current.LocateMaterialTheme<MaterialThemeBase>();
      var theme = materialTheme.CurrentTheme;

      if (isDark)
        theme.SetBaseTheme(Theme.Light);
      else
        theme.SetBaseTheme(Theme.Dark);

      materialTheme.CurrentTheme = theme;


    }


    public void RefreshCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Refresh" } });
      ApiUtils.ClearCache();
      Init();
    }


  }


}