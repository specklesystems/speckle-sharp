using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamCreateDialogViewModel : Conductor<IScreen>.Collection.OneActive
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
      set => SetAndNotify(ref _selectedFilter, value);
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
        var box = new StreamBox()
        {
          accountId = client.AccountId,
          client = client,
          filter = SelectedFilter,
          stream = StreamToCreate
        };
        _bindings.AddNewStream(box);
        var boxes = _bindings.GetFileClients();

        SelectedSlide = 3;
        _events.Publish(new StreamAddedEvent() { NewStream = StreamToCreate });
      }
      catch (Exception e)
      {
        Notifications.Enqueue($"Error: {e.Message}");
      }

      CreateButtonLoading = false;
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

    public void GetSelectedObjects()
    {
      if ( SelectedFilter == null )
      {
        Notifications.Enqueue("pls click one of the filter types!"); return;
      }
      if ( SelectedFilter.Type == typeof(ElementsSelectionFilter).ToString() )
      {
        var selectedObjs = _bindings.GetSelectedObjects();
        SelectedFilter.Selection = selectedObjs;
        Notifications.Enqueue($"Yay, you've added {selectedObjs.Count} objects!");
      }
      else
      {
        Notifications.Enqueue("soz this only works for selection!");
      }
    }
  }
}
