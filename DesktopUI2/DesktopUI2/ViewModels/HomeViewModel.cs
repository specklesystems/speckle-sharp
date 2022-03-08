using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Views;
using Material.Dialog;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public ReactiveCommand<string, Unit> RemoveSavedStreamCommand { get; }

    private int _selectedTab;
    public int SelectedTab
    {
      get => _selectedTab;
      private set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    private bool _showProgress;
    public bool InProgress
    {
      get => _showProgress;
      private set => this.RaiseAndSetIfChanged(ref _showProgress, value);
    }

    private List<Stream> _streams;
    public List<Stream> Streams
    {
      get => _streams;
      private set => this.RaiseAndSetIfChanged(ref _streams, value);
    }

    public bool HasSavedStreams => SavedStreams != null && SavedStreams.Any();

    private string _searchQuery;

    public string SearchQuery
    {
      get => _searchQuery;
      set
      {
        this.RaiseAndSetIfChanged(ref _searchQuery, value);
        SearchStreams().ConfigureAwait(false);
      }
    }

    //public Stream SelectedStream
    //{
    //  set
    //  {
    //    if (value != null)
    //    {
    //      var streamState = new StreamState(SelectedAccount, value);
    //      OpenStream(value, streamState);
    //    }
    //  }
    //}

    private ObservableCollection<SavedStreamViewModel> _savedStreams = new ObservableCollection<SavedStreamViewModel>();
    public ObservableCollection<SavedStreamViewModel> SavedStreams
    {
      get => _savedStreams;
      set
      {
        this.RaiseAndSetIfChanged(ref _savedStreams, value);
        this.RaisePropertyChanged("HasSavedStreams");
      }
    }

    private List<Account> _accounts;
    public List<Account> Accounts
    {
      get => _accounts;
      private set => this.RaiseAndSetIfChanged(ref _accounts, value);
    }


    private Account _selectedAccount;
    public Account SelectedAccount
    {
      get => _selectedAccount;
      set
      {
        //the account will be cached, so no need to include it in future operations
        Analytics.TrackEvent(SelectedAccount, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Account Select" } });
        this.RaiseAndSetIfChanged(ref _selectedAccount, value);
        if (value != null)
          GetStreams().ConfigureAwait(false); //update streams
      }
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
    }

    /// <summary>
    /// This get usually triggered on file open or view activated
    /// </summary>
    /// <param name="streams"></param>
    internal void UpdateSavedStreams(List<StreamState> streams)
    {
      SavedStreams.CollectionChanged -= SavedStreams_CollectionChanged;
      SavedStreams = new ObservableCollection<SavedStreamViewModel>();
      streams.ForEach(x => SavedStreams.Add(new SavedStreamViewModel(x, HostScreen, RemoveSavedStreamCommand)));
      this.RaisePropertyChanged("HasSavedStreams");
      SavedStreams.CollectionChanged += SavedStreams_CollectionChanged;
      SelectedTab = SavedStreams.Any() ? 1 : 0;
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

    internal void AddSavedStream(StreamState streamState, bool send = false, bool receive = false)
    {
      //saved stream has been edited
      var savedState = SavedStreams.FirstOrDefault(x => x.StreamState.Id == streamState.Id);
      if (savedState != null)
      {
        savedState.StreamState = streamState;
        WriteStreamsToFile();
      }
      //it's a new saved stream
      else
      {
        savedState = new SavedStreamViewModel(streamState, HostScreen, RemoveSavedStreamCommand);
        SavedStreams.Add(savedState);
      }

      this.RaisePropertyChanged("HasSavedStreams");
      SelectedTab = 1;

      //for save&send and save&receive
      if (send)
        savedState.SendCommand();

      if (receive)
        savedState.ReceiveCommand();
    }

    private async Task GetStreams()
    {

      InProgress = true;
      try
      {
        var client = new Client(SelectedAccount);
        Streams = await client.StreamsGet();
      }
      catch (Exception e)
      {
        Dialogs.ShowDialog("Something went wrong...", e.Message, Material.Dialog.Icons.DialogIconKind.Error);
      }
      finally
      {
        InProgress = false;
      }
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
      try
      {
        var client = new Client(SelectedAccount);
        Streams = await client.StreamSearch(SearchQuery);
      }
      catch (Exception)
      {
        // search prob returned no results
        Streams = new List<Stream>();
      }
      finally
      {
        InProgress = false;
      }
    }

    internal void Init()
    {
      Accounts = AccountManager.GetAccounts().ToList();

      if (!Accounts.Any())
      {
        ShowNoAccountPopup();
        return;
      }

      SelectedAccount = AccountManager.GetDefaultAccount();

      ////restore saved streams list
      //Bindings.SavedStreamsStates.ForEach(x => new SavedStreamViewModel(x, HostScreen, RemoveSavedStreamCommand));
      ////propagate changes back to bindings
      //SavedStreams.CollectionChanged += (s, e) => Bindings.SavedStreamsStates = SavedStreams.Select(x => x.StreamState).ToList();
    }

    private void RemoveSavedStream(string id)
    {
      var s = SavedStreams.FirstOrDefault(x => x.StreamState.Id == id);
      if (s != null)
      {
        SavedStreams.Remove(s);
        Tracker.TrackPageview("stream", "remove");
        Analytics.TrackEvent(SelectedAccount, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Remove" } });
      }

      this.RaisePropertyChanged("HasSavedStreams");
    }

    private async void ShowNoAccountPopup()
    {
      var btn = new Button()
      {
        Content = "Launch Manager...",
        Margin = new Avalonia.Thickness(20)
      };
      btn.Click += LaunchManagerBtnClick;
      await DialogHelper.CreateCustomDialog(new CustomDialogBuilderParams()
      {
        ContentHeader = "No account found!",
        WindowTitle = "No account found!",
        DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Error,
        StartupLocation = WindowStartupLocation.CenterOwner,
        NegativeResult = new DialogResult("retry"),
        Borderless = true,
        MaxWidth = MainWindow.Instance.Width - 40,
        DialogButtons = new DialogResultButton[]
          {
            new DialogResultButton
            {
              Content = "TRY AGAIN",
              Result = "retry"
            },

          },
        Content = btn
      }).ShowDialog(MainWindow.Instance);

      Init();
    }

    private void LaunchManagerBtnClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        Process.Start(@"/Applications/SpeckleManager.app");
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        Process.Start(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "speckle-manager", "SpeckleManager.exe"));
      }

    }

    public void ClearSearchCommand()
    {
      SearchQuery = "";
    }
    public void ViewOnlineCommand(object parameter)
    {
      var stream = parameter as Stream;
      Process.Start(new ProcessStartInfo($"{SelectedAccount.serverInfo.url.TrimEnd('/')}/streams/{stream.id}") { UseShellExecute = true });
      Tracker.TrackPageview(Tracker.STREAM_VIEW);
      Analytics.TrackEvent(SelectedAccount, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream View" } });
    }

    public void SendCommand(object parameter)
    {
      var streamState = new StreamState(SelectedAccount, parameter as Stream);
      OpenStream(streamState);
    }

    public void ReceiveCommand(object parameter)
    {
      var streamState = new StreamState(SelectedAccount, parameter as Stream) { IsReceiver = true };
      OpenStream(streamState);
    }

    public async void NewStreamCommand()
    {
      var dialog = DialogHelper.CreateTextFieldDialog(new TextFieldDialogBuilderParams()
      {
        ContentHeader = "Create a new Stream",
        SupportingText = "Create a new Stream by providing a name and description. New Streams are private by default.",
        WindowTitle = "Create new Stream",
        StartupLocation = WindowStartupLocation.CenterOwner,
        Borderless = true,
        Width = MainWindow.Instance.Width - 40,
        TextFields = new TextFieldBuilderParams[]
        {
          new TextFieldBuilderParams
          {
              Classes = "Outline",
              Label = "Name",
              HelperText = "* Required",
              MaxCountChars = 150,
              Validater = ValidateName
          },
           new TextFieldBuilderParams
          {
              Label = "Description",
              Classes = "Outline",
          }
        },

        PositiveButton = new DialogResultButton
        {
          Content = "CREATE",
          Result = "create"
        },
        NegativeButton = new DialogResultButton
        {
          Content = "CANCEL",
          Result = "cancel"
        },
      });

#if DEBUG
      dialog.GetWindow().AttachDevTools(KeyGesture.Parse("CTRL+R"));
#endif

      var result = await dialog.ShowDialog(MainWindow.Instance);

      if (result.GetResult == "create")
      {
        var name = result.GetFieldsResult()[0].Text;
        var description = result.GetFieldsResult()[1].Text;

        try
        {
          var client = new Client(SelectedAccount);
          var streamId = await client.StreamCreate(new StreamCreateInput { description = description, name = name, isPublic = false });
          var stream = await client.StreamGet(streamId);
          var streamState = new StreamState(SelectedAccount, stream);

          OpenStream(streamState);

          Tracker.TrackPageview(Tracker.STREAM_CREATE);
          Analytics.TrackEvent(SelectedAccount, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Create" } });

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

      var dialog = DialogHelper.CreateTextFieldDialog(new TextFieldDialogBuilderParams()
      {
        ContentHeader = "Add stream by URL",
        SupportingText = "You can use a Stream, Branch, or Commit URL.",
        WindowTitle = "Add stream by URL",
        StartupLocation = WindowStartupLocation.CenterOwner,
        Borderless = true,
        Width = MainWindow.Instance.Width - 40,
        TextFields = new TextFieldBuilderParams[]
        {
          new TextFieldBuilderParams
          {
              Classes = "Outline",
              Label = "URL",
              Validater = ValidateUrl,
              DefaultText = defaultText
          },
        },
        PositiveButton = new DialogResultButton
        {
          Content = "ADD",
          Result = "add"
        },
        NegativeButton = new DialogResultButton
        {
          Content = "CANCEL",
          Result = "cancel"
        },
      });

#if DEBUG
      dialog.GetWindow().AttachDevTools(KeyGesture.Parse("CTRL+R"));
#endif

      var result = await dialog.ShowDialog(MainWindow.Instance);

      if (result.GetResult == "add")
      {
        var url = result.GetFieldsResult()[0].Text;

        try
        {
          var sw = new StreamWrapper(url);
          var account = await sw.GetAccount();
          var client = new Client(account);
          var stream = await client.StreamGet(sw.StreamId);
          var streamState = new StreamState(account, stream);

          OpenStream(streamState);

          Tracker.TrackPageview("stream", "add-from-url");
          Analytics.TrackEvent(SelectedAccount, Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Stream Add From URL" } });
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
      MainWindowViewModel.RouterInstance.Navigate.Execute(new StreamEditViewModel(HostScreen, streamState));
    }

  }
}