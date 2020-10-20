using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Credentials;

namespace Speckle.DesktopUI.Accounts
{
  public class AccountsRepository
  {
    public ObservableCollection<Account> LoadAccounts()
    {
      return new ObservableCollection<Account>(AccountManager.GetAccounts());
    }
    
    public Account GetDefault()
    {
      return AccountManager.GetDefaultAccount();
    }
  }
}
