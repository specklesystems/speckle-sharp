using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.DesignScript.Runtime;
using Sentry;
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
    /// <param name="transports">Transports to send the data to</param>
    /// <returns name="log">Log</returns>
    public static List<string> Send(Base data, List<ITransport> transports, CancellationToken cancellationToken,
      Dictionary<ITransport, string> branchNames = null, string message = "",
      Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null)
    {
      var commitWrappers = new List<string>();
      var responses = new List<string>();

      var objectId = Operations.Send(data, cancellationToken, new List<ITransport>(transports), true,
        onProgressAction, onErrorAction).Result;

      if (cancellationToken.IsCancellationRequested)
        return null;

      foreach (var t in transports)
      {
        // Only create commits on ServerTransport instances (for now)
        if (!(t is ServerTransport serverTransport))
        {
          //commitWrappers.Add(t + objectId);
          continue;
        }
        var branchName = branchNames == null ? "main" : branchNames[t];
        var client = new Client(serverTransport.Account);
        try
        {
          var res = client.CommitCreate(cancellationToken,
            new CommitCreateInput
            {
              streamId = serverTransport.StreamId,
              branchName = branchName,
              objectId = objectId,
              message = message,
              sourceApplication = Utils.GetAppName(),
              parents = new List<string> { serverTransport.StreamId }
            }).Result;

          responses.Add(res);
          var wrapper =
            new StreamWrapper(serverTransport.StreamId, serverTransport.Account.userInfo.id, serverTransport.BaseUri)
            {
              CommitId = res
            };
          commitWrappers.Add(wrapper.ToString());
          Analytics.TrackEvent(client.Account, Analytics.Events.Send);
        }
        catch (Exception ex)
        {
          Utils.HandleApiExeption(ex);
          return null;
        }

      }

      return commitWrappers;
    }

    public static object SendData(string output)
    {
      var commits = output.Split('|').ToList();
      if (commits.Count == 1)
        return commits[0];

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
      var account = stream.GetAccount().Result;
      //

      var client = new Client(account);
      Commit commit = null;

      if (stream.Type == StreamWrapperType.Stream || stream.Type == StreamWrapperType.Branch)
      {
        stream.BranchName = string.IsNullOrEmpty(stream.BranchName) ? "main" : stream.BranchName;

        try
        {
          var branch = client.BranchGet(cancellationToken, stream.StreamId, stream.BranchName, 1).Result;
          if (!branch.commits.items.Any())
          {
            throw new SpeckleException("No commits found.");
          }

          commit = branch.commits.items[0];
        }
        catch
        {
          throw new SpeckleException("No branch found with name " + stream.BranchName);
        }
      }
      else if (stream.Type == StreamWrapperType.Commit)
      {
        try
        {
          commit = client.CommitGet(cancellationToken, stream.StreamId, stream.CommitId).Result;
        }
        catch (Exception ex)
        {
          Utils.HandleApiExeption(ex);
          return null;
        }
      }
      else if (stream.Type == StreamWrapperType.Object)
      {
        commit = new Commit() { referencedObject = stream.ObjectId, id = Guid.NewGuid().ToString() };
      }

      if (commit == null)
      {
        throw new SpeckleException("Could not get commit.");
      }

      if (cancellationToken.IsCancellationRequested)
        return null;

      var transport = new ServerTransport(account, stream.StreamId);

      var @base = Operations.Receive(
        commit.referencedObject,
        cancellationToken,
        remoteTransport: transport,
        onProgressAction: onProgressAction,
        onErrorAction: ((s, exception) => throw exception),
        onTotalChildrenCountKnown: onTotalChildrenCountKnown,
        disposeTransports: true
      ).Result;

      if (@base == null)
      {
        throw new SpeckleException("Receive operation returned nothing", false);
      }
      try
      {
        client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream.StreamId,
          commitId = commit?.id,
          message = commit?.message,
          sourceApplication = HostApplications.Dynamo.GetVersion(HostAppVersion.vRevit)
        }).Wait();
      }
      catch
      {
        // Do nothing!
      }

      if (cancellationToken.IsCancellationRequested)
        return null;

      var converter = new BatchConverter();
      converter.OnError += (sender, args) => onErrorAction?.Invoke("C", args.Error);
      
      var data = converter.ConvertRecursivelyToNative(@base);
      
      Analytics.TrackEvent(client.Account, Analytics.Events.Receive, new Dictionary<string, object>()
      {
        { "sourceHostApp", HostApplications.GetHostAppFromString(commit.sourceApplication)?.Slug },
        { "sourceHostAppVersion", commit.sourceApplication }
      });

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
