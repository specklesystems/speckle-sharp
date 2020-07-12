using System;
using System.Linq;
using NUnit.Framework;
using Speckle.Credentials;

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

      AccountManager.UpdateOrSaveAccount(TestAccount1);
      AccountManager.UpdateOrSaveAccount(TestAccount2);
    }

    [TearDown]
    public void TearDown()
    {
      AccountManager.DeleteLocalAccount(TestAccount1.id);
      AccountManager.DeleteLocalAccount(TestAccount2.id);
    }

    [Test]
    public void GetAllAccounts()
    {
      var accs = AccountManager.GetAllAccounts();
      Assert.GreaterOrEqual(accs.Count(), 2); // Tests are adding two accounts, you might have extra accounts on your machine when testing :D 
    }

    [Test]
    public void GetAccountsForServer()
    {
      var accs = AccountManager.GetServerAccounts("baz").ToList();

      Assert.AreEqual(1, accs.Count);
      Assert.AreEqual("qux", accs[0].serverInfo.company);
    }

    [Test]
    public void UpdateAccount()
    {
      AccountManager.SetDefaultAccount(TestAccount2.id);
      var acc = AccountManager.GetDefaultAccount();
      Assert.AreEqual(TestAccount2.userInfo.email, acc.userInfo.email);

      AccountManager.SetDefaultAccount(TestAccount1.id);
      var acc2 = AccountManager.GetDefaultAccount();
      Assert.AreEqual(TestAccount1.id, acc2.id);
    }
  }
}
