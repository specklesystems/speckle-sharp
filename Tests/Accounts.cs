using System;
using System.Linq;
using NUnit.Framework;
using Speckle.Core;

namespace Tests
{
  [TestFixture]
  public class Accounts
  {
    Account TestAccount;

    [SetUp]
    public void SetUp()
    {
      TestAccount = new Account()
      {
        ApiToken = "Butt",
        ServerName = "SERVER BOT DOUND",
        Email = "dimitrie@speckle.systems",
        ServerUrl = "https://hesita.speckle.works"
      };

      Account.SaveAccount(TestAccount);
    }

    [TearDown]
    public void TearDown()
    {
      Account.DeleteAccount(TestAccount.Id);
    }

    [Test]
    public void CreateAndGetAccount()
    {
      var acc = new Account()
      {
        ApiToken = "Butt",
        ServerName = "SERVER BOT DOUND",
        Email = "dimitrie@speckle.systems",
        ServerUrl = "https://hesita.speckle.works",
        Id = "GarbageAccount"
      };

      Account.SaveAccount(acc);

      var accGet = Account.GetAccount(acc.Id);

      Assert.NotNull(accGet);
      Assert.AreEqual(accGet.Id, acc.Id);
    }

    [Test]
    public void GetAllAccounts()
    {
      var accs = Account.GetLocalAccounts();

      Assert.GreaterOrEqual(accs.Count(), 1);
    }
  }
}
