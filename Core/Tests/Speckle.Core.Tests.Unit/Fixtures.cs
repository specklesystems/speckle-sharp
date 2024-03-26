using Newtonsoft.Json;
using NUnit.Framework;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Unit;

[SetUpFixture]
public class SetUp
{
  public static SpeckleLogConfiguration TestLogConfig { get; } =
    new(logToFile: false, logToSeq: false, logToSentry: false);

  [OneTimeSetUp]
  public void BeforeAll()
  {
    SpeckleLog.Initialize("Core", "Testing", TestLogConfig);
    SpeckleLog.Logger.Information("Initialized logger for testing");
  }
}

public abstract class Fixtures
{
  private static readonly SQLiteTransport s_accountStorage = new(scope: "Accounts");

  private static readonly string s_accountPath = Path.Combine(
    SpecklePathProvider.AccountsFolderPath,
    "TestAccount.json"
  );

  public static void UpdateOrSaveAccount(Account account)
  {
    DeleteLocalAccount(account.id);
    string serializedObject = JsonConvert.SerializeObject(account);
    s_accountStorage.SaveObjectSync(account.id, serializedObject);
  }

  public static void SaveLocalAccount(Account account)
  {
    var json = JsonConvert.SerializeObject(account);
    File.WriteAllText(s_accountPath, json);
  }

  public static void DeleteLocalAccount(string id)
  {
    s_accountStorage.DeleteObject(id);
  }

  public static void DeleteLocalAccountFile()
  {
    File.Delete(s_accountPath);
  }
}
