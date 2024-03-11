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

    yield return new TestCaseData(givenAccounts, "https://new.example.com", new[] { newAccount })
      .SetName("Get New")
      .SetDescription("When requesting for new account, ensure only this account is returned");

    yield return new TestCaseData(givenAccounts, "https://old.example.com", new[] { newAccount, oldAccount }) //TODO: Maybe we want this without duplicates
      .SetName("Get New via Old")
      .SetDescription("When requesting for old account, ensure migrated account is returned first");

    var reversed = Enumerable.Reverse(givenAccounts).ToList();

    yield return new TestCaseData(reversed, "https://old.example.com", new[] { newAccount, oldAccount })
      .SetName("Get New via Old (Reversed order)")
      .SetDescription("Account order shouldn't matter");
  }

  [Test]
  [TestCaseSource(nameof(MigrationTestCase))]
  public void TestServerMigration(IList<Account> accounts, string requestedUrl, IList<Account> expectedSequence)
  {
    AddAccounts(accounts);

    var result = AccountManager.GetAccounts(requestedUrl).ToList();

    Assert.That(result, Is.EquivalentTo(expectedSequence));
  }

  [TearDown]
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

  private void AddAccounts(IEnumerable<Account> accounts)
  {
    foreach (Account account in accounts)
    {
      _accountsToCleanUp.Add(account);
      Fixtures.UpdateOrSaveAccount(account);
    }
  }
}
