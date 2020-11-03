using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;

namespace TestsUnit
{
  public class Fixtures
  {
    private static SQLiteTransport AccountStorage = new SQLiteTransport(scope: "Accounts");

    public static void UpdateOrSaveAccount(Account account)
    {
      AccountStorage.DeleteObject(account.id);
      AccountStorage.SaveObjectSync(account.id, JsonConvert.SerializeObject(account));
    }

    public static void DeleteLocalAccount(string id)
    {
      AccountStorage.DeleteObject(id);
    }
  }
}
