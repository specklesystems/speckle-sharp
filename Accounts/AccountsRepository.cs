using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.DesktopUI.Accounts
{
  class AccountsRepository
  {
    public ObservableCollection<Account> LoadTestAccounts()
    {
      List<Account> TestAccounts = new List<Account>()
      {
        new Account()
        {
          isDefault = true,
          serverInfo = new ServerInfo()
          {
            company = "Speckle",
            name = "Hella Cool Server",
            url = "http://localhost:3000/"
          },
          userInfo = new UserInfo()
          {
            name = Environment.UserName,
            company = "Testing Desktop UI Inc",
            id = "user123",
            email = "testing@speckle.systems"
          },
          token = Environment.GetEnvironmentVariable("speckle2_dev_token")
        },
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
      return new ObservableCollection<Account>(AccountManager.GetAccounts());
    }

    public ObservableCollection<Account> LoadNonDefaultAccounts()
    {
      var accounts = new ObservableCollection<Account>();
      foreach (var acc in LoadAccounts())
      {
        if (!acc.isDefault)
        {
          accounts.Add(acc);
        }
      }
      return accounts;
    }

    public void RemoveAccount(string id)
    {
      AccountManager.DeleteLocalAccount(id);
    }

    public Account GetDefault()
    {
      return AccountManager.GetDefaultAccount();
    }

    public void SetDefault(Account account)
    {
      AccountManager.SetDefaultAccount(account.id);
    }

    public async Task<Account> AuthenticateAccount(string serverUrl)
    {
      return await AccountManager.AuthenticateConnectors(serverUrl);
    }
  }
}
