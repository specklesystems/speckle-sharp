using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Api
{
  public static class Helpers
  {
    /// <summary>
    /// Helper method to Receive from a Speckle Server.
    /// </summary>
    /// <param name="stream">Stream URL or Id to receive from. If the URL contains branchName, commitId or objectId those will be used, otherwise the latest commit from main will be received.</param>
    /// <param name="account">Account to use. If not provided the default account will be used.</param>
    /// <param name="onProgressAction">Action invoked on progress iterations.</param>
    /// <param name="onErrorAction">Action invoked on internal errors.</param>
    /// <param name="onTotalChildrenCountKnown">Action invoked once the total count of objects is known.</param>
    /// <returns></returns>
    public static async Task<Base> Receive(string stream, Account account = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null, Action<int> onTotalChildrenCountKnown = null)
    {
      var sw = new StreamWrapper(stream);

      var client = new Client(account ?? await sw.GetAccount());

      var transport = new ServerTransport(client.Account, sw.StreamId);

      string objectId = "";
      Commit commit = null;

      //OBJECT URL
      if (!string.IsNullOrEmpty(sw.ObjectId))
      {
        objectId = sw.ObjectId;
      }

      //COMMIT URL
      else if (!string.IsNullOrEmpty(sw.CommitId))
      {
        commit = await client.CommitGet(sw.StreamId, sw.CommitId);
        objectId = commit.referencedObject;
      }

      //BRANCH URL OR STREAM URL
      else
      {
        var branchName = string.IsNullOrEmpty(sw.BranchName) ? "main" : sw.BranchName;

        var branch = await client.BranchGet(sw.StreamId, branchName, 1);
        if (!branch.commits.items.Any())
          throw new SpeckleException($"The selected branch has no commits.", level: SentryLevel.Info);

        commit = branch.commits.items[0];
        objectId = branch.commits.items[0].referencedObject;
      }

      Analytics.TrackEvent(client.Account, Analytics.Events.Receive);

      var receiveRes = await Operations.Receive(
        objectId,
        remoteTransport: transport,
        onErrorAction: onErrorAction,
        onProgressAction: onProgressAction,
        onTotalChildrenCountKnown: onTotalChildrenCountKnown,
        disposeTransports: true
      );

      try
      {
        await client.CommitReceived(new CommitReceivedInput
        {
          streamId = sw.StreamId,
          commitId = commit?.id,
          message = commit?.message,
          sourceApplication = "Other"
        });
      }
      catch
      {
        // Do nothing!
      }
      return receiveRes;
    }

    /// <summary>
    /// Helper method to Send to a Speckle Server.
    /// </summary>
    /// <param name="stream">Stream URL or Id to send to. If the URL contains branchName, commitId or objectId those will be used, otherwise the latest commit from main will be received.</param>
    /// <param name="data">Data to send</param>
    /// <param name="account">Account to use. If not provided the default account will be used.</param>
    /// <param name="useDefaultCache">Toggle for the default cache. If set to false, it will only send to the provided transports.</param>
    /// <param name="onProgressAction">Action invoked on progress iterations.</param>
    /// <param name="onErrorAction">Action invoked on internal errors.</param>
    /// <returns></returns>
    public static async Task<string> Send(string stream, Base data, string message = "No message", string sourceApplication = ".net", int totalChildrenCount = 0, Account account = null, bool useDefaultCache = true, Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null)
    {
      var sw = new StreamWrapper(stream);

      var client = new Client(account ?? await sw.GetAccount());

      var transport = new ServerTransport(client.Account, sw.StreamId);
      var branchName = string.IsNullOrEmpty(sw.BranchName) ? "main" : sw.BranchName;

      var objectId = await Operations.Send(
        data,
        new List<ITransport> { transport },
        useDefaultCache,
        onProgressAction,
        onErrorAction, disposeTransports: true);

      Analytics.TrackEvent(client.Account, Analytics.Events.Send);

      return await client.CommitCreate(
            new CommitCreateInput
            {
              streamId = sw.StreamId,
              branchName = branchName,
              objectId = objectId,
              message = message,
              sourceApplication = sourceApplication,
              totalChildrenCount = totalChildrenCount,
            });

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="slug">The connector slug eg. revit, rhino, etc</param>
    /// <returns></returns>
    public static async Task<bool> IsConnectorUpdateAvailable(string slug)
    {
#if DEBUG
      //when debugging the version is not correct, so don't bother
      return false;
#endif

      try
      {
        var latestUrl = $"https://releases.speckle.dev/installers/{slug}/latest.yml";
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(latestUrl);
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        Version latestVersion = null;

        using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
        using (System.IO.Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
          var res = await reader.ReadToEndAsync();
          latestVersion = new Version(res.Replace("version:", "").Trim());
        }

        var currentVersion = Assembly.GetAssembly(typeof(Helpers)).GetName().Version;

        if (latestVersion > currentVersion)
          return true;
      }
      catch (Exception ex)
      {
        new SpeckleException($"Could not check for connector updates: {slug}", ex, true, SentryLevel.Warning);
      }

      return false;
    }
  }
}

