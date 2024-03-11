using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Unit.Credentials;

public class AccountServerMigrationTests
{
  private List<Account> _accountsToCleanUp = new();

  public static IEnumerable<TestCaseData> MigrationTestCase()
  {
    Account oldAccount = CreateTestAccount("https://old.example.com", null, new("https://new.example.com"));
    Account newAccount = CreateTestAccount("https://new.example.com", new("https://old.example.com"), null);
    Account otherAccount = CreateTestAccount("https://other.example.com", null, null);

    List<Account> givenAccounts = new() { oldAccount, newAccount, otherAccount };
    List<Account> expectedResult = new() { newAccount, oldAccount };
    yield return new TestCaseData(givenAccounts, "https://new.example.com", expectedResult);
  }

  [Test]
  [TestCaseSource(nameof(MigrationTestCase))]
  public void TestServerMigration(List<Account> accounts, string requestedUrl, List<Account> expectedSequence)
  {
    AddAccounts(accounts);

    var result = AccountManager.GetAccounts(requestedUrl).ToList();

    Assert.That(result, Is.EquivalentTo(expectedSequence));
  }

  [OneTimeTearDown]
  public void TearDown()
  {
    //Clean up any of the test accounts we made
    foreach (var acc in _accountsToCleanUp)
    {
      Fixtures.DeleteAccount(acc);
    }
  }

  private static Account CreateTestAccount(string url, Uri movedFrom, Uri movedTo)
  {
    return new Account
    {
      token = "myToken",
      serverInfo = new ServerInfo
      {
        url = url,
        name = "myServer",
        migration = new ServerMigration { movedTo = movedTo, movedFrom = movedFrom }
      },
      userInfo = new UserInfo
      {
        id = new Guid().ToString(),
        email = "user@example.com",
        name = "user"
      }
    };
  }

  private void AddAccounts(List<Account> accounts)
  {
    foreach (Account account in accounts)
    {
      _accountsToCleanUp.Add(account);
      Fixtures.UpdateOrSaveAccount(account);
    }
  }
}
