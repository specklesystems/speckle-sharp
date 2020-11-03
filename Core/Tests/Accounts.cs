using System;
using System.Linq;
using NUnit.Framework;
using Speckle.Core.Credentials;
using TestsUnit;

namespace Tests
{
  [TestFixture]
  public class CredentialInfrastructure
  {
    Account TestAccount1, TestAccount2;

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

      Fixtures.UpdateOrSaveAccount(TestAccount1);
      Fixtures.UpdateOrSaveAccount(TestAccount2);
    }

    [TearDown]
    public void TearDown()
    {
      Fixtures.DeleteLocalAccount(TestAccount1.id);
      Fixtures.DeleteLocalAccount(TestAccount2.id);
    }

    [Test]
    public void GetAllAccounts()
    {
      var accs = AccountManager.GetAccounts();
      Assert.GreaterOrEqual(accs.Count(), 2); // Tests are adding two accounts, you might have extra accounts on your machine when testing :D 
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
  }
}
