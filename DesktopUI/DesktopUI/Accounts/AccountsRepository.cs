using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using Stylet;

namespace Speckle.DesktopUI.Accounts
{
  public class AccountsRepository
  {
    public BindableCollection<Account> LoadAccounts()
    {
      return new BindableCollection<Account>(AccountManager.GetAccounts());
    }

    public Account GetDefault()
    {
      return AccountManager.GetDefaultAccount();
    }
  }
}
