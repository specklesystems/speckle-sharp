using System;
using System.Linq;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using TestsUnit;

namespace Tests
{
  [TestFixture]
  public class CredentialInfrastructure
  {
    Account TestAccount1, TestAccount2;

    Account TestAccount3;

    [SetUp]
    public void SetUp()
    {
      TestAccount1 = new Account
      {
        refreshToken = "bla",
        token = "bla",
        serverInfo = new ServerInfo
        {
          url = "bla",
          company = "bla"
        },
        userInfo = new UserInfo
        {
          email = "one@two.com"
        }
      };

      TestAccount2 = new Account
      {
        refreshToken = "foo",
        token = "bar",
        serverInfo = new ServerInfo
        {
          url = "baz",
          company = "qux"
        },
        userInfo = new UserInfo
        {
          email = "three@four.com"
        }
      };

      TestAccount3 = new Account
      {
        token = "secret",
        serverInfo = new ServerInfo
        {
          url = "https://sample.com",
          name = "qux"
        },
        userInfo = new UserInfo
        {
          email = "six@five.com",
          id = "123345",
          name = "Test Account 3"
        }
      };



      Fixtures.UpdateOrSaveAccount(TestAccount1);
      Fixtures.UpdateOrSaveAccount(TestAccount2);
      Fixtures.SaveLocalAccount(TestAccount3);
    }

    [TearDown]
    public void TearDown()
    {
      Fixtures.DeleteLocalAccount(TestAccount1.id);
      Fixtures.DeleteLocalAccount(TestAccount2.id);
      Fixtures.DeleteLocalAccountFile();
    }

    [Test]
    public void GetAllAccounts()
    {
      var accs = AccountManager.GetAccounts();
      Assert.GreaterOrEqual(accs.Count(), 3); // Tests are adding three accounts, you might have extra accounts on your machine when testing :D 
    }

    [Test]
    public void GetAccountsForServer()
    {
      var accs = AccountManager.GetAccounts("baz").ToList();

      Assert.AreEqual(1, accs.Count);
      Assert.AreEqual("qux", accs[0].serverInfo.company);
      Assert.AreEqual("baz", accs[0].serverInfo.url);
      Assert.AreEqual("foo", accs[0].refreshToken);
    }

    [Test]
    public void GetLocalAccount()
    {
      var acc = AccountManager.GetAccounts().Where(x => x.userInfo.id == "123345").FirstOrDefault();

      Assert.AreEqual("https://sample.com", acc.serverInfo.url);
      Assert.AreEqual("secret", acc.token);
    }
  }
}
