using Newtonsoft.Json;
using NUnit.Framework;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Transports;

namespace Tests;

[SetUpFixture]
public class SetUp
{
  [OneTimeSetUp]
  public void BeforeAll()
  {
    SpeckleLog.Initialize("Core", "Testing", new SpeckleLogConfiguration(logToFile: false, logToSeq: false));
    SpeckleLog.Logger.Information("Initialized logger for testing");
  }
}

public abstract class Fixtures
{
  private static SQLiteTransport AccountStorage = new(scope: "Accounts");

  private static string accountPath = Path.Combine(SpecklePathProvider.AccountsFolderPath, "TestAccount.json");

  public static void UpdateOrSaveAccount(Account account)
  {
    AccountStorage.DeleteObject(account.id);
    string serializedObject = JsonConvert.SerializeObject(account);
    AccountStorage.SaveObjectSync(account.id, serializedObject);
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
