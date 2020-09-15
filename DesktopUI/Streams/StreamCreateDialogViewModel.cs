using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    private IViewModelFactory _viewModelFactory;
    private IEventAggregator _events;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public StreamCreateDialogViewModel(
      IViewModelFactory viewModelFactory,
      IEventAggregator events)
    {
      DisplayName = "Create Stream";
      _viewModelFactory = viewModelFactory;
      _events = events;
    }

    private StreamsRepository _repo = new StreamsRepository();
    private AccountsRepository _acctRepo = new AccountsRepository();

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

    public async void CreateStream()
    {
      CreateButtonLoading = true;
      try
      {
        string streamId = await _repo.CreateStream(StreamToCreate, AccountToSendFrom);
        StreamToCreate = await _repo.GetStream(streamId, AccountToSendFrom);
        SelectedSlide = 3;
        _events.Publish(new StreamAddedEvent()
        {
          NewStream = StreamToCreate
        });
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
  }
}