using System;
using System.Linq;
using NUnit.Framework;
//using Speckle.Core;

namespace Tests
{
  [TestFixture]
  public class Accounts
  {
    //Account TestAccount;

    //[SetUp]
    //public void SetUp()
    //{
    //  TestAccount = new Account()
    //  {
    //    ApiToken = "Butt",
    //    ServerName = "SERVER BOT DOUND",
    //    Email = "dimitrie@speckle.systems",
    //    ServerUrl = "https://hesita.speckle.works"
    //  };

    //  Account.SaveAccount(TestAccount);
    //}

    //[TearDown]
    //public void TearDown()
    //{
    //  Account.DeleteAccount(TestAccount.Id);
    //}

    //[Test]
    //public void CreateAndGetAccount()
    //{
    //  var acc = new Account()
    //  {
    //    ApiToken = "SuckThisToken",
    //    ServerName = "SERVER BOT DOUND",
    //    Email = "dimitrie@speckle.systems",
    //    ServerUrl = "https://hesita.speckle.works",
    //    Id = "GarbageAccount"
    //  };

    //  Account.SaveAccount(acc);

    //  var accGet = Account.GetAccount(acc.Id);

    //  Assert.NotNull(accGet);
    //  Assert.AreEqual(accGet.Id, acc.Id);
    //}

    //[Test]
    //public void GetAllAccounts()
    //{
    //  var accs = Account.GetLocalAccounts();

    //  Assert.GreaterOrEqual(accs.Count(), 1);
    //}

    //[Test]
    //public void UpdateAccount()
    //{
    //  TestAccount.Email = "spammeister@woot.com";
    //  Account.UpdateAccount(TestAccount);

    //  var acc = Account.GetLocalAccounts().First(acc => acc.Email == "spammeister@woot.com");
    //  Assert.NotNull(acc);
    //}

    //[Test]
    //public void DefaultAccount()
    //{
    //  var acc1 = new Account()
    //  {
    //    ApiToken = "SuckThisToken",
    //    ServerName = "SERVER BOT DOUND",
    //    Email = "dimitrie@speckle.systems",
    //    ServerUrl = "https://hesita.speckle.works"
    //  };
    //  Account.SaveAccount(acc1);


    //  var acc2 = new Account()
    //  {
    //    ApiToken = "SuckThisToken",
    //    ServerName = "SUPER DUPER",
    //    Email = "dimitrie@speckle.systems",
    //    ServerUrl = "https://hesita.speckle.works"
    //  };
    //  Account.SaveAccount(acc2);

    //  var acc3 = TestAccount;

    //  Account.SetDefaultAccount(acc3.Id);

    //  Assert.AreEqual(Account.GetDefaultAccount().Id, acc3.Id);

    //  Account.SetDefaultAccount(TestAccount.Id);

    //  Assert.AreEqual(Account.GetDefaultAccount().Id, TestAccount.Id);

    //  Account.ClearDefaultAccount();

    //  Assert.IsNull(Account.GetDefaultAccount());

    //}
  }
}
