using SpeckleDesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using Speckle.Credentials;

namespace SpeckleDesktopUI.Accounts
{
    class AccountsViewModel : BindableBase
    {
        public AccountsViewModel()
        {
            _repo = new AccountsRepository();
            AccountsList = _repo.LoadTestAccounts();

            RemoveCommand = new RelayCommand<Account>(OnRemove);
        }
        private AccountsRepository _repo;
        private Account _defaultAccount;
        private Account _selectedAccount;
        private ObservableCollection<Account> _accountsList;
        public ObservableCollection<Account> AccountsList
        {
            get => _accountsList;
            set => SetProperty(ref _accountsList, value);
        }
        public Account DefaultAccount
        {
            get => _defaultAccount;
            set => SetProperty(ref _defaultAccount, value);
        }
        public Account SelectedAccount
        {
            get => _selectedAccount;
            set => SetProperty(ref _selectedAccount, value);
        }

        public RelayCommand<Account> RemoveCommand { get; set; }
        private void OnRemove(Account account)
        {
            _repo.RemoveAccount(account.id);
            AccountsList = _repo.LoadAccounts();
        }
    }
}
