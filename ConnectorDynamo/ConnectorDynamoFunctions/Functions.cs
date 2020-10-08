using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Functions
{
  /// <summary>
  /// Speckle methods
  /// </summary>
  
  public static class Functions
  {
    /// <summary>
    /// Sends data to a Speckle Server by creating a commit on the master branch of a Stream
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="streamId">Stream ID to send the data to</param>
    /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
    /// <returns name="log">Log</returns>
    [IsVisibleInDynamoLibrary(false)]
    public static string Send([ArbitraryDimensionArrayImport] object data, string streamId, Core.Credentials.Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();
      var client = new Client(account);
      var @base = Utils.ConvertRecursivelyToSpeckle(data);
      var transport = new ServerTransport(account, streamId);
      var objectId = Operations.Send(@base, new List<ITransport>() { transport }).Result;

      var res = client.CommitCreate(new CommitCreateInput
      {
        streamId = streamId,
        branchName = "master",
        objectId = objectId,
        message = "Automatic commit from Dynamo"
      }).Result;

      if (!string.IsNullOrEmpty(res))
        return "Sent successfully @ " + DateTime.Now.ToShortTimeString();
      return null;
    }

    /// <summary>
    /// Receives data from a Speckle Server by getting the last commit on the master branch of a Stream
    /// </summary>
    /// <param name="streamId">Stream ID to receive the last commit from</param>
    /// <param name="account">Speckle account to use, if not provided the default account will be used</param>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    public static object Receive(string streamId, Core.Credentials.Account account = null)
    {
      if (account == null)
        account = AccountManager.GetDefaultAccount();

      var client = new Client(account);
      var res = client.StreamGet(streamId).Result;
      if (res == null || !res.branches.items[0].commits.items.Any())
        return null;

      var lastCommit = res.branches.items[0].commits.items[0];

      var transport = new ServerTransport(account, streamId);
      var @base = Operations.Receive(lastCommit.referencedObject, remoteTransport: transport).Result;
      var data = Utils.ConvertRecursivelyToNative(@base);

      return data;
    }




  }
}
