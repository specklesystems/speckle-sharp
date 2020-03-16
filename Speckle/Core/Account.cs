using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Speckle.Transports;

namespace Speckle.Core
{
  /// <summary>
  /// A users's remote server account.
  /// </summary>
  public partial class Account
  {
    private static DiskTransport AccountTransport = new DiskTransport(scope: "Accounts", splitPath: false);

    public string Id { get; set; } = Guid.NewGuid().ToString().ToLower();

    public string Email { get; set; }

    public string ServerName { get; set; }

    public string ServerUrl { get; set; }

    public string ApiToken { get; set; }

    public bool Default { get; set; } = false;

    public Account() { }

    /// <summary>
    /// Gets all local accounts. Default storage is under AppData/Speckle/Accounts.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<Account> GetLocalAccounts()
    {
      var allAccountStrings = AccountTransport.GetAllObjects();
      foreach(var accountString in allAccountStrings)
      {
        yield return JsonConvert.DeserializeObject<Account>(accountString);
      }
    }

    /// <summary>
    /// Gets the default account. Returns null if none exists.
    /// </summary>
    /// <returns></returns>
    public static Account GetDefaultAccount()
    {
      return GetLocalAccounts().FirstOrDefault(acc => acc.Default);
    }

    /// <summary>
    /// Sets the specified account to be the default one.
    /// </summary>
    /// <param name="accountId"></param>
    public static void SetDefaultAccount(string accountId)
    {
      ClearDefaultAccount();
      var acc = GetAccount(accountId);
      acc.Default = true;
      SaveAccount(acc);
    }

    /// <summary>
    /// Clears any default account if present.
    /// </summary>
    public static void ClearDefaultAccount()
    {
      foreach(var acc in GetLocalAccounts())
      {
        acc.Default = false;
        SaveAccount(acc);
      }
    }

    /// <summary>
    /// Saves an account to the user's app data folder.
    /// </summary>
    /// <param name="account"></param>
    public static void SaveAccount(Account account)
    {
      AccountTransport.SaveObject(account.Id, JsonConvert.SerializeObject(account), true);
    }

    public static void SetDefaultAccount(object id)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Deletes an account.
    /// </summary>
    /// <param name="id"></param>
    public static void DeleteAccount(string id)
    {
      AccountTransport.RemoveObject(id);
    }

    /// <summary>
    /// Gets an account by its id.
    /// </summary>
    /// <param name="accountId"></param>
    /// <returns></returns>
    public static Account GetAccount(string accountId)
    {
      return JsonConvert.DeserializeObject<Account>(AccountTransport.GetObject(accountId));
    }

    /// <summary>
    /// Updates an account.
    /// </summary>
    /// <param name="account"></param>
    public static void UpdateAccount(Account account)
    {
      AccountTransport.SaveObject(account.Id, JsonConvert.SerializeObject(account), true);
    }
  }
}
