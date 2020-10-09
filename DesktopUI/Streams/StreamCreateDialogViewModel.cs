using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamCreateDialogViewModel : Conductor<IScreen>.Collection.OneActive,
    IHandle<RetrievedFilteredObjectsEvent>
  {
    private IEventAggregator _events;
    private ConnectorBindings _bindings;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public StreamCreateDialogViewModel(
      IEventAggregator events,
      ConnectorBindings bindings)
    {
      DisplayName = "Create Stream";
      _events = events;
      _bindings = bindings;
      _filters = new BindableCollection<ISelectionFilter>(_bindings.GetSelectionFilters());
    }

    private StreamsRepository _repo => new StreamsRepository();
    private AccountsRepository _acctRepo => new AccountsRepository();

    public ISnackbarMessageQueue Notifications
    {
      get => _notifications;
      set => SetAndNotify(ref _notifications, value);
    }

    public string ActiveViewName
    {
      get => _bindings.GetActiveViewName();
    }

    public List<string> ActiveViewObjects
    {
      get => _bindings.GetObjectsInView();
    }

    public List<string> CurrentSelection
    {
      get => _bindings.GetSelectedObjects();
    }

    private bool _createButtonLoading;

    public bool CreateButtonLoading
    {
      get => _createButtonLoading;
      set => SetAndNotify(ref _createButtonLoading, value);
    }

    private Stream _streamToCreate = new Stream();

    public Stream StreamToCreate
    {
      get => _streamToCreate;
      set => SetAndNotify(ref _streamToCreate, value);
    }

    private StreamState _streamState = new StreamState();

    public StreamState StreamState
    {
      get => _streamState;
      set => SetAndNotify(ref _streamState, value);
    }

    private Account _accountToSendFrom;

    public Account AccountToSendFrom
    {
      get => _accountToSendFrom;
      set => SetAndNotify(ref _accountToSendFrom, value);
    }

    private BindableCollection<ISelectionFilter> _filters;

    public BindableCollection<ISelectionFilter> Filters
    {
      get => new BindableCollection<ISelectionFilter>(_filters);
      set => SetAndNotify(ref _filters, value);
    }

    private ISelectionFilter _selectedFilter;

    public ISelectionFilter SelectedFilter
    {
      get => _selectedFilter;
      set
      {
        SetAndNotify(ref _selectedFilter, value);
        NotifyOfPropertyChange(nameof(CanGetSelectedObjects));
      }
    }

    public ObservableCollection<Account> Accounts
    {
      get => _acctRepo.LoadAccounts();
    }

    private int _selectedSlide = 0;

    public int SelectedSlide
    {
      get => _selectedSlide;
      set => SetAndNotify(ref _selectedSlide, value);
    }

    private string _userQuery;

    public string UserQuery
    {
      get => _userQuery;
      set
      {
        SetAndNotify(ref _userQuery, value);
        SearchForUsers();
      }
    }

    private BindableCollection<User> _userSearchResults;

    public BindableCollection<User> UserSearchResults
    {
      get => _userSearchResults;
      set => SetAndNotify(ref _userSearchResults, value);
    }

    private User _selectedUser;

    public User SelectedUser
    {
      get => _selectedUser;
      set => SetAndNotify(ref _selectedUser, value);
    }

    public void ContinueStreamCreate(string slideIndex)
    {
      if (StreamToCreate.name == null || StreamToCreate.name.Length < 2)
      {
        Notifications.Enqueue("Please choose a name for your stream!");
        return;
      }

      AccountToSendFrom = _acctRepo.GetDefault();
      ChangeSlide(slideIndex);
    }

    public async void AddNewStream()
    {
      CreateButtonLoading = true;
      try
      {
        var client = new Client(AccountToSendFrom);
        var streamId = await _repo.CreateStream(StreamToCreate, AccountToSendFrom);
        // TODO do this locally first before creating on the server
        StreamToCreate = await _repo.GetStream(streamId, AccountToSendFrom);
        StreamState = new StreamState()
        {
          accountId = client.AccountId,
          client = client,
          filter = SelectedFilter,
          stream = StreamToCreate
        };
        _bindings.AddNewStream(StreamState);
        var boxes = _bindings.GetFileContext();

        SelectedSlide = 3;
        _events.Publish(new StreamAddedEvent() { NewStream = StreamState });
      }
      catch (Exception e)
      {
        Notifications.Enqueue($"Error: {e.Message}");
      }

      CreateButtonLoading = false;
    }

    public async void SearchForUsers()
    {
      if (UserQuery.Length <= 2)
        return;

      try
      {
        var client = new Client(AccountToSendFrom);
        var users = await client.UserSearch(UserQuery);
        UserSearchResults = new BindableCollection<User>(users);
      }
      catch (Exception e)
      {
        Debug.WriteLine(e);
      }
    }

    public void AddSimpleStream()
    {
      SelectedFilter = Filters.First(filter => filter.Type == typeof(ElementsSelectionFilter).ToString());
      GetSelectedObjects();
      AccountToSendFrom = _acctRepo.GetDefault();

      AddNewStream();
    }

    public void AddStreamFromView()
    {
      SelectedFilter = Filters.First(filter => filter.Type == typeof(ElementsSelectionFilter).ToString());
      SelectedFilter.Selection = ActiveViewObjects;

      AddNewStream();
    }

    // TODO extract dialog logic into separate manager to better handle open / close
    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }

    public void ChangeSlide(string slideIndex)
    {
      SelectedSlide = int.Parse(slideIndex);
    }

    public bool CanGetSelectedObjects
    {
      get => SelectedFilter != null;
    }

    public void GetSelectedObjects()
    {
      if (SelectedFilter == null)
      {
        Notifications.Enqueue("pls click one of the filter types!");
        return;
      }

      if (SelectedFilter.Type == typeof(ElementsSelectionFilter).ToString())
      {
        var selectedObjs = _bindings.GetSelectedObjects();
        SelectedFilter.Selection = selectedObjs;
        NotifyOfPropertyChange(nameof(SelectedFilter.Selection.Count));
      }
      else
      {
        Notifications.Enqueue("soz this only works for selection!");
      }
    }

    public void Handle(RetrievedFilteredObjectsEvent message)
    {
      StreamState.placeholders = message.Objects as List<Base>;
      // Notifications.Enqueue(message.Notification);
    }
  }
}
