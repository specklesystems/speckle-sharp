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
    private AccountsRepository _accountsRepo;
    private ConnectorBindings _bindings;

    public StreamsHomeViewModel(IViewModelFactory viewModelFactory,
      AccountsRepository accountsRepo, ConnectorBindings bindings)
    {
      DisplayName = "Home";
      _viewModelFactory = viewModelFactory;
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
        // just restart the app - easier than doing an events hokey pokey to get test streams reloaded
        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
        Application.Current.Shutdown();
        return;
      }
      _bindings.RaiseNotification("Sorry, no local accounts were found 😢");
    }
  }
}
