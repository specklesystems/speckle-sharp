using System.Linq;
using Speckle.Core.Credentials;

namespace DUI3.Utils;

public static class Accounts
{
  public static Account GetAccount(string accountId)
  {
    Account account =
      AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == accountId)
      ?? throw new SpeckleAccountManagerException();
    return account;
  }
}
