using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Unit.Credentials;

[TestFixture]
public class CredentialInfrastructure
{
  [SetUp]
  public void SetUp()
  {
    _testAccount1 = new Account
    {
      refreshToken = "bla",
      token = "bla",
      serverInfo = new ServerInfo { url = "bla", company = "bla" },
      userInfo = new UserInfo { email = "one@two.com" }
    };

    _testAccount2 = new Account
    {
      refreshToken = "foo",
      token = "bar",
      serverInfo = new ServerInfo { url = "baz", company = "qux" },
      userInfo = new UserInfo { email = "three@four.com" }
    };

    _testAccount3 = new Account
    {
      token = "secret",
      serverInfo = new ServerInfo { url = "https://sample.com", name = "qux" },
      userInfo = new UserInfo
      {
        email = "six@five.com",
        id = "123345",
        name = "Test Account 3"
      }
    };

    Fixtures.UpdateOrSaveAccount(_testAccount1);
    Fixtures.UpdateOrSaveAccount(_testAccount2);
    Fixtures.SaveLocalAccount(_testAccount3);
  }

  [TearDown]
  public void TearDown()
  {
    Fixtures.DeleteLocalAccount(_testAccount1.id);
    Fixtures.DeleteLocalAccount(_testAccount2.id);
    Fixtures.DeleteLocalAccountFile();
  }

  private Account _testAccount1,
    _testAccount2,
    _testAccount3;

  [Test]
  public void GetAllAccounts()
  {
    var accs = AccountManager.GetAccounts().ToList();
    Assert.That(accs, Has.Count.GreaterThanOrEqualTo(3)); // Tests are adding three accounts, you might have extra accounts on your machine when testing :D
  }

  [Test]
  public void GetAccountsForServer()
  {
    var accs = AccountManager.GetAccounts("baz").ToList();

    Assert.That(accs, Has.Count.EqualTo(1));
    Assert.That(accs[0].serverInfo.company, Is.EqualTo("qux"));
    Assert.That(accs[0].serverInfo.url, Is.EqualTo("baz"));
    Assert.That(accs[0].refreshToken, Is.EqualTo("foo"));
  }

  [Test]
  public void GetLocalAccount()
  {
    var acc = AccountManager.GetAccounts().FirstOrDefault(x => x.userInfo.id == "123345");

    Assert.That(acc, Is.Not.Null);
    Assert.That(acc.serverInfo.url, Is.EqualTo("https://sample.com"));
    Assert.That(acc.token, Is.EqualTo("secret"));
  }

  [Test]
  public void EnsureLocalIdentifiers_AreUniqueAcrossServers()
  {
    // Accounts with the same user ID in different servers should always result in different local identifiers.
    string id = "12345";
    var acc1 = new Account
    {
      serverInfo = new ServerInfo { url = "https://speckle.xyz" },
      userInfo = new UserInfo { id = id }
    }.GetLocalIdentifier();

    var acc2 = new Account
    {
      serverInfo = new ServerInfo { url = "https://app.speckle.systems" },
      userInfo = new UserInfo { id = id }
    }.GetLocalIdentifier();

    Assert.That(acc1, Is.Not.EqualTo(acc2));
  }
}
