using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.Converter.Dynamo;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo
{
  /// <summary>
  /// Speckle methods
  /// </summary>
  public static class Speckle
  {

    /// <summary>
    /// Your default Speckle account
    /// </summary>
    /// <returns name="account">Your default Speckle account</returns>
    [NodeName("Default Account")]
    [NodeCategory("Speckle")]
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
    [NodeName("Accounts")]
    [NodeCategory("Speckle")]
    [NodeDescription("Your Speckle accounts")]
    [NodeSearchTags("accounts", "speckle")]
    public static IEnumerable<Account> Accounts()
    {
      return AccountManager.GetAccounts();

    }



    /// <summary>
    /// Sends data to Speckle
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="streamId">Stream ID to send the data to</param>
    /// <param name="account">Speckle account to use</param>
    /// <returns name="log">Log</returns>
    [NodeName("Send")]
    [NodeCategory("Speckle")]
    [NodeDescription("Sends data to Speckle")]
    [NodeSearchTags("send", "speckle")]
    public static string Send(object data, string streamId, [DefaultArgument("null")] Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var converter = new ConverterDynamo();
      var spkbase = converter.ConvertToSpeckle(data);

      var client = new Client(account);

      var commitId = Operations.Send(spkbase, streamId, client).Result;

      var res = client.CommitCreate(new CommitCreateInput
      {
        streamId = streamId,
        branchName = "master",
        objectId = commitId,
        message = "Automatic commit from Dynamo"
      }).Result;

      if (!string.IsNullOrEmpty(res))
        return "Sent successfully @ " + DateTime.Now.ToShortTimeString();
      return null;
    }

    /// <summary>
    /// Sends data to Speckle
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <returns name="localDataId">The ID of the local data sent</returns>
    [NodeName("SendLocal")]
    [NodeCategory("Speckle")]
    [NodeDescription("Sends data locally")]
    [NodeSearchTags("send", "speckle")]
    public static string SendLocal(object data)
    {
      var converter = new ConverterDynamo();
      var spkbase = converter.ConvertToSpeckle(data);
      var commitId = Operations.Send(spkbase).Result;

      return commitId;
    }


    /// <summary>
    /// Receives data from Speckle
    /// </summary>
    /// <param name="streamId">Stream ID to receive data from</param>
    /// <param name="account">Speckle account to use</param>
    /// <returns name="data">Data received</returns>
    [NodeName("Receive")]
    [NodeCategory("Speckle")]
    [NodeDescription("Receives data from Speckle")]
    [NodeSearchTags("receive", "speckle")]
    public static object Receive(string streamId, [DefaultArgument("null")] Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var client = new Client(account);
      var res = client.StreamGet(streamId).Result;
      var lastCommit = res.branches.items[0].commits.items[0];

      var converter = new ConverterDynamo();

      var spkbase = Operations.Receive(lastCommit.referencedObject, streamId, client).Result;
      var data = converter.ConvertToNative(spkbase);

      return data;

    }

    /// <summary>
    /// Receives data locally
    /// </summary>
    /// <param name="localDataId">The ID of the local data to receive</param>
    /// <returns name="data">Data received</returns>
    [NodeName("ReceiveLocal")]
    [NodeCategory("Speckle")]
    [NodeDescription("Receives data locally")]
    [NodeSearchTags("receive", "speckle")]
    public static object ReceiveLocal(string localDataId)
    {
      var converter = new ConverterDynamo();
      var spkbase = Operations.Receive(localDataId).Result;
      var data = converter.ConvertToNative(spkbase);

      return data;

    }


  }
}
