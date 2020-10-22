using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;


namespace Speckle.ConnectorDynamo.Functions
{
  public static class Account
  {
    // /// <summary>
    // /// Your default Speckle account
    // /// </summary>
    // /// <returns name="account">Your default Speckle account</returns>
    // [NodeCategory("Query")]
    // public static Core.Credentials.Account Default()
    // {
    //   Tracker.TrackEvent(Tracker.ACCOUNT_DEFAULT);
    //
    //   return AccountManager.GetDefaultAccount();
    // }

    // /// <summary>
    // /// Your Speckle accounts
    // /// </summary>
    // /// <returns name="accounts">Your Speckle accounts</returns>
    // [NodeCategory("Query")]
    // public static IEnumerable<Core.Credentials.Account> List()
    // {
    //   Tracker.TrackEvent(Tracker.ACCOUNT_LIST);
    //
    //   return AccountManager.GetAccounts();
    // }

    [IsVisibleInDynamoLibrary(false)]
    public static Core.Credentials.Account GetById(string id)
    {
      return AccountManager.GetAccounts().FirstOrDefault(x => x.id == id);
    }

    /// <summary>
    /// Get an Account details
    /// </summary>
    [NodeCategory("Query")]
    [MultiReturn(new[] {"id", "isDefault", "serverInfo", "userInfo"})]
    public static Dictionary<string, object> Details(Core.Credentials.Account account)
    {
      Tracker.TrackEvent(Tracker.ACCOUNT_DETAILS);

      return new Dictionary<string, object>
      {
        {"id", account.id},
        {"isDefault", account.isDefault},
        {
          "serverInfo",
          new Dictionary<string, string>
          {
            {"name", account.serverInfo.name},
            {"company", account.serverInfo.company},
            {"url", account.serverInfo.url}
          }
        },
        {
          "userInfo", new Dictionary<string, string>
          {
            {"id", account.userInfo.id},
            {"name", account.userInfo.name},
            {"email", account.userInfo.email},
            {"company", account.userInfo.company},
          }
        }
      };
    }
  }
}
