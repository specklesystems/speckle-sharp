using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.ConnectorDynamo.Functions;
using Speckle.ConnectorDynamo.Functions.Extras;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Functions
{
  /// <summary>
  /// Functions that are to be called by NodeModel nodes
  /// </summary>

  public static class Functions
  {
    /// <summary>
    /// Sends data to a Speckle Server by creating a commit on the master branch of a Stream
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="stream">Stream to send the data to</param>
    /// <returns name="log">Log</returns>
    [IsVisibleInDynamoLibrary(false)]
    public static string Send([ArbitraryDimensionArrayImport] object data, StreamWrapper stream, string branchName = null, string message = null)
    {
      Core.Credentials.Account account = stream.GetAccount();

      var client = new Client(account);
      var conversionResult = Utils.ConvertRecursivelyToSpeckle(data);
      var transport = new ServerTransport(account, stream.StreamId);
      var objectId = Operations.Send(conversionResult.Object, new List<ITransport>() { transport }).Result;

      branchName = null ?? "main";
      var plural = (conversionResult.TotalObjects == 1) ? "" : "s";
      message = null ?? $"Sent {conversionResult.TotalObjects} object{plural} from Dynamo";

      var res = client.CommitCreate(new CommitCreateInput
      {
        streamId = stream.StreamId,
        branchName = branchName,
        objectId = objectId,
        message = message
      }).Result;

      if (!string.IsNullOrEmpty(res))
        return "Sent successfully @ " + DateTime.Now.ToShortTimeString();
      return null;
    }

    /// <summary>
    /// Receives data from a Speckle Server by getting the last commit on the master branch of a Stream
    /// </summary>
    /// <param name="stream">Stream to receive from</param>
    /// <returns></returns>
    [IsVisibleInDynamoLibrary(false)]
    [MultiReturn(new[] { "data", "info" })]
    public static Dictionary<string, object> Receive(StreamWrapper stream)
    {
      Core.Credentials.Account account = stream.GetAccount();

      var client = new Client(account);
      var res = client.StreamGet(stream.StreamId).Result;
      if (res == null || !res.branches.items[0].commits.items.Any())
        return null;

      var lastCommit = res.branches.items[0].commits.items[0];

      var transport = new ServerTransport(account, stream.StreamId);
      var @base = Operations.Receive(lastCommit.referencedObject, remoteTransport: transport).Result;
      var data = Utils.ConvertRecursivelyToNative(@base);

      return new Dictionary<string, object> { { "data", data }, { "info", $"{lastCommit.authorName} @ {lastCommit.createdAt}: { lastCommit.message} (id:{lastCommit.id})" } };
    }




  }
}
