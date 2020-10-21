using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.DesignScript.Runtime;
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
  [IsVisibleInDynamoLibrary(false)]
  public static class Functions
  {
    /// <summary>
    /// Sends data to a Speckle Server by creating a commit on the master branch of a Stream
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="stream">Stream to send the data to</param>
    /// <returns name="log">Log</returns>
    public static string Send(Base data, StreamWrapper stream, string branchName = "main", string message = "", Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null)
    {
      Core.Credentials.Account account = stream.GetAccount();

      var client = new Client(account);
      var transport = new ServerTransport(account, stream.StreamId);
      var objectId = Operations.Send(data, new List<ITransport>() { transport }, true, onProgressAction, onErrorAction).Result;

      branchName = string.IsNullOrEmpty(branchName) ? "main" : branchName;

      var res = client.CommitCreate(new CommitCreateInput
      {
        streamId = stream.StreamId,
        branchName = branchName,
        objectId = objectId,
        message = message
      }).Result;

      return res;
    }

    /// <summary>
    /// Receives data from a Speckle Server by getting the last commit on the master branch of a Stream
    /// </summary>
    /// <param name="stream">Stream to receive from</param>
    /// <returns></returns>
    [MultiReturn(new[] { "data", "commit" })]
    public static Dictionary<string, object> Receive(StreamWrapper stream, string branchName, CancellationToken cancellationToken, Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null, Action<int> onTotalChildrenCountKnown = null)
    {
      Core.Credentials.Account account = stream.GetAccount();
      branchName = string.IsNullOrEmpty(branchName) ? "main" : branchName;

      var client = new Client(account);
      var res = client.StreamGet(stream.StreamId).Result;
      var mainBranch = res.branches.items.FirstOrDefault(b => b.name == branchName);

      if (mainBranch == null)
      {
        Log.CaptureAndThrow(new Exception("No branch found with name " + branchName));
      }

      if (res == null || !mainBranch.commits.items.Any())
        return null;

      var lastCommit = mainBranch.commits.items[0];

      var transport = new ServerTransport(account, stream.StreamId);
      var @base = Operations.Receive(
        lastCommit.referencedObject,
        cancellationToken,
        remoteTransport: transport,
        onProgressAction: onProgressAction,
        onErrorAction: onErrorAction,
        onTotalChildrenCountKnown: onTotalChildrenCountKnown

        ).Result;
      var converter = new BatchConverter();
      var data = converter.ConvertRecursivelyToNative(@base);

      return new Dictionary<string, object> { { "data", data }, { "commit", lastCommit } };
    }


    public static object ReceiveData(string inMemoryDataId)
    {
      return InMemoryCache.Get(inMemoryDataId)["data"];
    }

    public static string ReceiveInfo(string inMemoryDataId)
    {
      var commit = InMemoryCache.Get(inMemoryDataId)["commit"] as Commit;
      return $"{commit.authorName} @ {commit.createdAt}: { commit.message} (id:{commit.id})";

    }


  }
}
