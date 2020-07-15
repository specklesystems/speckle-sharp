using SpeckleDesktopUI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Credentials;
using System.Collections.ObjectModel;

namespace SpeckleDesktopUI.Accounts
{
    class AccountModel : BindableBase
    {
        private bool _isDefault;
        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }

        public ObservableCollection<Account> LoadTestAccounts()
        {
            List<Account> TestAccounts = new List<Account>(){
                new Account
                {
                    refreshToken = "fresh",
                    token = "cool token",
                    serverInfo = new ServerInfo { name = "cool server", url = "https://cool.speckle.com"},
                    userInfo = new UserInfo { name = "dingle", email = "me@cool.com" }
                },
                new Account
                {
                    refreshToken = "clean",
                    token = "good token",
                    serverInfo = new ServerInfo { name = "good server", url = "https://good.speckle.net"},
                    userInfo = new UserInfo { name = "dongle", email = "me@good.net" }
                },
                new Account
                {
                    refreshToken = "wow",
                    token = "dope token",
                    serverInfo = new ServerInfo { name = "dope server", url = "https://dope.speckle.gov"},
                    userInfo = new UserInfo { name = "dangle", email = "me@dope.gov" }
                }
            };
            TestAccounts.ForEach(acc => AccountManager.UpdateOrSaveAccount(acc));

            return LoadAccounts();
        }

        public ObservableCollection<Account> LoadAccounts()
        {
            return new ObservableCollection<Account>(AccountManager.GetAllAccounts());
        }
    }
}
