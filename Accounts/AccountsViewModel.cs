using SpeckleDesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using SpeckleCore;
using System.Collections.ObjectModel;
using System.Windows;
using Speckle.Credentials;

namespace SpeckleDesktopUI.Accounts
{
    class AccountsViewModel : BindableBase
    {
        public AccountsViewModel()
        {
            var accountModel = new AccountModel();
            AccountsList = accountModel.LoadTestAccounts();
        }
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

        public void LoadAccounts()
        {
            //AccountsList = new ObservableCollection<Account>(LocalContext.GetAllAccounts());
            //DefaultAccount = LocalContext.GetDefaultAccount();

            //AccountsList = new ObservableCollection<Account>(AccountManager.GetAllAccounts());
            //DefaultAccount = AccountManager.GetDefaultAccount();
        }
    }
}
