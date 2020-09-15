using System;
using System.Collections.ObjectModel;
using Speckle.Core.Credentials;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Accounts
{
  class AccountsViewModel : Screen
  {
    public AccountsViewModel()
    {
      _repo = new AccountsRepository();
#if DEBUG
      AllAccounts = _repo.LoadTestAccounts();
#else
      AllAccounts = _repo.LoadAccounts();
#endif
      AccountsNonDefault = _repo.LoadNonDefaultAccounts();
      DefaultAccount = _repo.GetDefault();
      SetDefaultCommand = new RelayCommand<Account>(OnSetDefault);
      RemoveCommand = new RelayCommand<Account>(OnRemove);
      AuthenticateCommand = new RelayCommand<string>(OnAuthenticate);
    }
    private AccountsRepository _repo;
    private Account _defaultAccount;
    private Account _selectedAccount;
    private ObservableCollection<Account> _allAccounts;
    private ObservableCollection<Account> _accountsNonDefault;
    public ObservableCollection<Account> AllAccounts
    {
      get => _allAccounts;
      set => SetAndNotify(ref _allAccounts, value);
    }
    public ObservableCollection<Account> AccountsNonDefault
    {
      get => _accountsNonDefault;
      set => SetAndNotify(ref _accountsNonDefault, value);
    }
    public Account DefaultAccount
    {
      get => _defaultAccount;
      set => SetAndNotify(ref _defaultAccount, value);
    }
    public Account SelectedAccount
    {
      get => _selectedAccount;
      set => SetAndNotify(ref _selectedAccount, value);
    }

    // TODO: handle when commands fail (prob with a dialogue popup)
    public RelayCommand<Account> RemoveCommand { get; set; }
    public RelayCommand<Account> SetDefaultCommand { get; set; }
    public RelayCommand<string> AuthenticateCommand { get; set; }
    private void OnRemove(Account account)
    {
      _repo.RemoveAccount(account.id);
      AccountsNonDefault = _repo.LoadNonDefaultAccounts();
    }

    private void OnSetDefault(Account account)
    {
      _repo.SetDefault(account);
      DefaultAccount = _repo.GetDefault();
      AccountsNonDefault = _repo.LoadNonDefaultAccounts();
    }

    private void OnAuthenticate(string serverUrl)
    {
      var acc = _repo.AuthenticateAccount(serverUrl);
    }
  }
}