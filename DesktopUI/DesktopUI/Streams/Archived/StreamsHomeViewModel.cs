using System;
using System.Linq;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamsHomeViewModel : Conductor<IScreen>.StackNavigation
  {
    private IViewModelFactory _viewModelFactory;
    private readonly IEventAggregator _events;
    private AccountsRepository _accountsRepo;
    private ConnectorBindings _bindings;

    public StreamsHomeViewModel(IViewModelFactory viewModelFactory, IEventAggregator events,
      AccountsRepository accountsRepo, ConnectorBindings bindings)
    {
      DisplayName = bindings.GetApplicationHostName() + " streams";
      _viewModelFactory = viewModelFactory;
      _events = events;
      _accountsRepo = accountsRepo;
      _bindings = bindings;
      _accounts = _accountsRepo.LoadAccounts();

      var item = _viewModelFactory.CreateAllStreamsViewModel();

      ActivateItem(item);
    }

    private  BindableCollection<Account> _accounts;

    public BindableCollection<Account> Accounts
    {
      get => _accounts;
      set => SetAndNotify(ref _accounts, value);
    }

    public void OpenManagerLink()
    {
      Link.OpenInBrowser("https://github.com/specklesystems/speckle-sharp/tree/master/DesktopUI#accounts");
    }

    public void ReloadAccounts()
    {
      Accounts = _accountsRepo.LoadAccounts();
      if ( Accounts.Any() )
      {
        _events.PublishOnUIThread(new ReloadRequestedEvent());
        _bindings.RaiseNotification($"Success! You have {Accounts.Count} local accounts.");
        return;
      }

      _bindings.RaiseNotification("Sorry, no local accounts were found 😢");
    }
  }
}
