using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Converter.Dynamo;
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
  [IsVisibleInDynamoLibrary(false)]
  public static class Functions
  {
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
    public static string Send([ArbitraryDimensionArrayImport] object data, string streamId, [DefaultArgument("null")] Account account = null)
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
    /// Sends data to Speckle
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <returns name="localDataId">The ID of the local data sent</returns>
    [NodeName("SendLocal")]
    [NodeCategory("Speckle")]
    [NodeDescription("Sends data locally")]
    [NodeSearchTags("send", "speckle")]
    public static string SendLocal([ArbitraryDimensionArrayImport] object data)
    {
      var @base = Utils.ConvertRecursivelyToSpeckle(data);
      var objectId = Operations.Send(@base).Result;

      return objectId;
    }



    [IsVisibleInDynamoLibrary(false)]
    public static object Receive(string streamId, Account account = null)
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

    /// <summary>
    /// Receives data locally
    /// </summary>
    /// <param name="localDataId">The ID of the local data to receive</param>
    /// <returns name="data">Data received</returns>
    [NodeName("ReceiveLocal")]
    [NodeCategory("Speckle")]
    [NodeDescription("Receives data locally")]
    [NodeSearchTags("receive", "speckle")]
    [IsVisibleInDynamoLibrary(true)]
    public static object ReceiveLocal(string localDataId)
    {
      var converter = new ConverterDynamo();
      var @base = Operations.Receive(localDataId).Result;
      var data = Utils.ConvertRecursivelyToNative(@base);
      return data;
    }


  }
}
