using NUnit.Framework;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Unit.Credentials;

public class AccountServerMigrationTests
{
  private readonly List<Account> _accountsToCleanUp = new();

  public static IEnumerable<TestCaseData> MigrationTestCase()
  {
    const string OLD_URL = "https://old.example.com";
    const string NEW_URL = "https://new.example.com";
    const string OTHER_URL = "https://other.example.com";
    Account oldAccount = CreateTestAccount(OLD_URL, null, new(NEW_URL));
    string accountId = oldAccount.userInfo.id; // new account user must match old account user id
    Account newAccount = CreateTestAccount(NEW_URL, new(OLD_URL), null, accountId);
    Account otherAccount = CreateTestAccount(OTHER_URL, null, null);

    List<Account> givenAccounts = new() { oldAccount, newAccount, otherAccount };

    yield return new TestCaseData(givenAccounts, NEW_URL, new[] { newAccount })
      .SetName("Get New")
      .SetDescription("When requesting for new account, ensure only this account is returned");

    yield return new TestCaseData(givenAccounts, OLD_URL, new[] { newAccount })
      .SetName("Get New via Old")
      .SetDescription("When requesting for old account, ensure migrated account is returned first");

    var reversed = Enumerable.Reverse(givenAccounts).ToList();

    yield return new TestCaseData(reversed, OLD_URL, new[] { newAccount })
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
      Fixtures.DeleteLocalAccount(acc.id);
    }
    _accountsToCleanUp.Clear();
  }

  private static Account CreateTestAccount(string url, Uri movedFrom, Uri movedTo, string id = null)
  {
    id ??= Guid.NewGuid().ToString();
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
        id = id,
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
