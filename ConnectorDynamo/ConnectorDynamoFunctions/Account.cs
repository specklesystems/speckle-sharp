using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorDynamo.Functions;

public static class Account
{
  [IsVisibleInDynamoLibrary(false)]
  public static Core.Credentials.Account GetById(string id)
  {
    var acc = AccountManager.GetAccounts().FirstOrDefault(x => x.userInfo.id == id);
    AnalyticsUtils.TrackNodeRun(acc, "Account Get");
    return acc;
  }

  /// <summary>
  /// Get an Account details
  /// </summary>
  [NodeCategory("Query")]
  [MultiReturn(new[] { "isDefault", "serverInfo", "userInfo" })]
  public static Dictionary<string, object> Details(Core.Credentials.Account account)
  {
    if (account == null)
    {
      Utils.HandleApiExeption(new WarningException("Provided account was invalid."));
    }

    AnalyticsUtils.TrackNodeRun(account, "Account Details");

    return new Dictionary<string, object>
    {
      { "isDefault", account.isDefault },
      {
        "serverInfo",
        new Dictionary<string, string>
        {
          { "name", account.serverInfo.name },
          { "company", account.serverInfo.company },
          { "url", account.serverInfo.url }
        }
      },
      {
        "userInfo",
        new Dictionary<string, string>
        {
          { "id", account.userInfo.id },
          { "name", account.userInfo.name },
          { "email", account.userInfo.email },
          { "company", account.userInfo.company },
        }
      }
    };
  }
}
