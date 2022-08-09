using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Speckle.Cor.Api;

namespace TestsUnit
{
  public class Fixtures
  {
    private static SQLiteTransport AccountStorage = new SQLiteTransport(scope: "Accounts");
    private static string accountPath = Path.Combine(Helpers.SpeckleFolderPath, "Accounts", "TestAccount.json");


    public static void UpdateOrSaveAccount(Account account)
    {
      AccountStorage.DeleteObject(account.id);
      AccountStorage.SaveObjectSync(account.id, JsonConvert.SerializeObject(account));
    }

    public static void SaveLocalAccount(Account account)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(accountPath));
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
