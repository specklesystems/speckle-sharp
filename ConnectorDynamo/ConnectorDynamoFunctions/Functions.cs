using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.DesignScript.Runtime;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
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
    public static List<string> Send(Base data, List<StreamWrapper> streams, CancellationToken cancellationToken,
      List<string> branchNames = null, string message = "",
      Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null)
    {
      var responses = new List<string>();
      var transports = new List<ITransport>();
      var accounts = new List<Core.Credentials.Account>();
      foreach (var stream in streams)
      {
        var account = stream.GetAccount();
        accounts.Add(account); //cached here
        transports.Add(new ServerTransport(account, stream.StreamId));
      }

      var objectId = Operations.Send(data, cancellationToken, transports, true,
        onProgressAction, onErrorAction).Result;

      if (cancellationToken.IsCancellationRequested)
        return null;

      for (int i = 0; i < streams.Count; i++)
      {
        var branchName = branchNames == null ? "main" : branchNames[i];
        var client = new Client(accounts[i]);

        try
        {
          var res = client.CommitCreate(cancellationToken,
         new CommitCreateInput
         {
           streamId = streams[i].StreamId,
           branchName = branchName,
           objectId = objectId,
           message = message,
           sourceApplication = Applications.Dynamo,
           parents = new List<string>() {streams[i].CommitId}
         }).Result;

          responses.Add(res);
        }
        catch (Exception ex)
        {
          Utils.HandleApiExeption(ex);
          return null;
        }

      }

      return responses;
    }

    public static object SendData(string output)
    {
      var commits = output.Split('|').Select(x => new StreamWrapper(x)).ToList();
      if (commits.Count() == 1)
        return commits[0];
      else
        return commits;
    }

    /// <summary>
    /// Receives data from a Speckle Server by getting the last commit on the master branch of a Stream
    /// </summary>
    /// <param name="stream">Stream to receive from</param>
    /// <returns></returns>
    [MultiReturn(new[] { "data", "commit" })]
    public static Dictionary<string, object> Receive(StreamWrapper stream, CancellationToken cancellationToken,
      Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null,
      Action<int> onTotalChildrenCountKnown = null)
    {
      Core.Credentials.Account account = stream.GetAccount();
      stream.BranchName = string.IsNullOrEmpty(stream.BranchName) ? "main" : stream.BranchName;

      var client = new Client(account);
      Commit commit = null;
      try
      {
        if (string.IsNullOrEmpty(stream.CommitId))
        {

          var branches = client.StreamGetBranches(cancellationToken, stream.StreamId).Result;
          var mainBranch = branches.FirstOrDefault(b => b.name == stream.BranchName);

          if (mainBranch == null)
          {
            Log.CaptureAndThrow(new Exception("No branch found with name " + stream.BranchName));
          }

          if (!mainBranch.commits.items.Any())
            throw new Exception("No commits found.");

          commit = mainBranch.commits.items[0];
        }
        else
        {
          commit = client.CommitGet(cancellationToken, stream.StreamId, stream.CommitId).Result;
        }
      }
      catch (Exception ex)
      {
        Utils.HandleApiExeption(ex);
        return null;
      }

      if (commit == null)
      {
        throw new Exception("Could not get commit.");
      }

      if (cancellationToken.IsCancellationRequested)
        return null;

      var transport = new ServerTransport(account, stream.StreamId);
      var @base = Operations.Receive(
        commit.referencedObject,
        cancellationToken,
        remoteTransport: transport,
        onProgressAction: onProgressAction,
        onErrorAction: onErrorAction,
        onTotalChildrenCountKnown: onTotalChildrenCountKnown
      ).Result;

      if (cancellationToken.IsCancellationRequested)
        return null;

      var converter = new BatchConverter();
      var data = converter.ConvertRecursivelyToNative(@base);

      return new Dictionary<string, object> { { "data", data }, { "commit", commit } };
    }


    public static object ReceiveData(string inMemoryDataId)
    {
      return InMemoryCache.Get(inMemoryDataId)["data"];
    }

    public static string ReceiveInfo(string inMemoryDataId)
    {
      var commit = InMemoryCache.Get(inMemoryDataId)["commit"] as Commit;
      return $"{commit.authorName} @ {commit.createdAt}: {commit.message} (id:{commit.id})";
    }
  }
}
