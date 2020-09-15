using System;
using System.Collections.Generic;
using Dynamo.Graph.Nodes;
using Speckle.Core.Credentials;


namespace Speckle.ConnectorDynamo.Functions
{
  public static class SpeckleAccounts
  {
    /// <summary>
    /// Your default Speckle account
    /// </summary>
    /// <returns name="account">Your default Speckle account</returns>
    [NodeName("Default Account")]
    [NodeCategory("Query")]
    [NodeDescription("Your default Speckle account")]
    [NodeSearchTags("account", "speckle")]
    public static Account DefaultAccount()
    {
      return AccountManager.GetDefaultAccount();

    }

    /// <summary>
    /// Your Speckle accounts
    /// </summary>
    /// <returns name="accounts">Your Speckle accounts</returns>
    [NodeName("Accounts BBB")]
    [NodeCategory("Create")]
    [NodeDescription("Your Speckle accounts")]
    [NodeSearchTags("accounts", "speckle")]
    public static IEnumerable<Account> Accounts()
    {
      return AccountManager.GetAccounts();

    }
  }
}
