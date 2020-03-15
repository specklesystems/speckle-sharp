using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Speckle.Transports;

namespace Speckle.Core
{
  public class Account
  {
    private static DiskTransport AccountTransport = new DiskTransport(scope: "Accounts", splitPath: false);

    public string Id { get; set; } = Guid.NewGuid().ToString().ToLower();

    public string Email { get; set; }

    public string ServerName { get; set; }

    public string ServerUrl { get; set; }

    public string ApiToken { get; set; }

    public Account() { }

    public static IEnumerable<Account> GetLocalAccounts()
    {
      var strs = AccountTransport.GetAllObjects();
      foreach(var s in strs)
      {
        yield return JsonConvert.DeserializeObject<Account>(s);
      }
    }

    public static void SaveAccount(Account account)
    {
      var accs = JsonConvert.SerializeObject(account);
      AccountTransport.SaveObject(account.Id, accs, true);
    }

    public static void DeleteAccount(string id)
    {
      AccountTransport.RemoveObject(id);
    }

    public static Account GetAccount(string accountId)
    {
      return JsonConvert.DeserializeObject<Account>(AccountTransport.GetObject(accountId));
    }

    public static void UpdateAccount(Account account)
    {
      throw new NotImplementedException();
    }
  }
}
