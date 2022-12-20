using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Transports;

namespace TestsUnit
{
  public class Fixtures
  {
    private static SQLiteTransport AccountStorage = new SQLiteTransport(scope: "Accounts");
    private static string accountPath = Path.Combine(SpecklePathProvider.AccountsFolderPath, "TestAccount.json");

    public static void UpdateOrSaveAccount(Account account)
    {
      AccountStorage.DeleteObject(account.id);
      AccountStorage.SaveObjectSync(account.id, JsonConvert.SerializeObject(account));
    }

    public static void SaveLocalAccount(Account account)
    {
      var json = JsonConvert.SerializeObject(account);
      File.WriteAllText(accountPath, json);
    }

    public static void DeleteLocalAccount(string id)
    {
      AccountStorage.DeleteObject(id);
    }

    public static void DeleteLocalAccountFile()
    {
      File.Delete(accountPath);
    }
  }
}
