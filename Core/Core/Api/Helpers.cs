using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Api
{
  public static class Helpers
  {
    public const string ReleasesUrl = "https://releases.speckle.dev";
    private static string _feedsEndpoint = ReleasesUrl + "/manager2/feeds";
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

      try
      {
        account ??= await sw.GetAccount();
      }
      catch (SpeckleException e)
      {
        if (string.IsNullOrEmpty(sw.StreamId)) throw e;

        //Fallback to a non authed account
        account = new Account()
        {
          token = "",
          serverInfo = new ServerInfo() { url = sw.ServerUrl },
          userInfo = new UserInfo()
        };
      }

      var client = new Client(account);

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

      Analytics.TrackEvent(client.Account, Analytics.Events.Receive, new Dictionary<string, object>()
          {
            { "sourceHostApp", HostApplications.GetHostAppFromString(commit.sourceApplication).Slug },
            { "sourceHostAppVersion", commit.sourceApplication }
          });

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
      if (slug == "dui2")
        slug = "revit";
      //when debugging the version is not correct, so don't bother
      return false;
#endif

      try
      {
        HttpClient client = Http.GetHttpProxyClient();
        var response = await client.GetStringAsync($"{_feedsEndpoint}/{slug}.json");
        var connector = JsonSerializer.Deserialize<Connector>(response);

        var os = Os.Win;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
          os = Os.OSX;

        var versions = connector.Versions.Where(x => x.Os == os).OrderByDescending(x => x.Date).ToList();
        var stables = versions.Where(x => !x.Prerelease);
        if (!stables.Any())
          return false;

        var latestVersion = new System.Version(stables.First().Number);

        var currentVersion = Assembly.GetAssembly(typeof(Helpers)).GetName().Version;

        if (latestVersion > currentVersion)
          return true;
      }
      catch (Exception ex)
      {
        //new SpeckleException($"Could not check for connector updates: {slug}", ex, true, SentryLevel.Warning);
      }

      return false;
    }


    public static string TimeAgo(string timestamp)
    {
      return TimeAgo(DateTime.Parse(timestamp));
    }
    public static string TimeAgo(DateTime timestamp)
    {
      TimeSpan timeAgo;
      try
      {
        timeAgo = DateTime.Now.Subtract(timestamp);
      }
      catch (FormatException e)
      {
        return "never";
      }

      if (timeAgo.TotalSeconds < 60)
        return "just now";
      if (timeAgo.TotalMinutes < 60)
        return $"{timeAgo.Minutes} minute{PluralS(timeAgo.Minutes)} ago";
      if (timeAgo.TotalHours < 24)
        return $"{timeAgo.Hours} hour{PluralS(timeAgo.Hours)} ago";
      if (timeAgo.TotalDays < 7)
        return $"{timeAgo.Days} day{PluralS(timeAgo.Days)} ago";
      if (timeAgo.TotalDays < 30)
        return $"{timeAgo.Days / 7} week{PluralS(timeAgo.Days / 7)} ago";
      if (timeAgo.TotalDays < 365)
        return $"{timeAgo.Days / 30} month{PluralS(timeAgo.Days / 30)} ago";

      return $"{timeAgo.Days / 356} year{PluralS(timeAgo.Days / 356)} ago";
    }

    public static string PluralS(int num)
    {
      return num != 1 ? "s" : "";
    }


    /// <summary>
    /// Returns the correct location of the Speckle installation folder. Usually this would be the user's %appdata%/Speckle folder, unless the install was made for all users.
    /// </summary>
    /// <returns>The location of the Speckle installation folder</returns>
    [Obsolete("Please use Helpers/SpecklePathProvider.InstallSpeckleFolderPath", true)]
    public static string InstallSpeckleFolderPath => Path.Combine(InstallApplicationDataPath, "Speckle");

    /// <summary>
    /// Returns the correct location of the Speckle folder for the current user. Usually this would be the user's %appdata%/Speckle folder.
    /// </summary>
    /// <returns>The location of the Speckle installation folder</returns>
    [Obsolete("Please use Helpers/SpecklePathProvider.UserSpeckleFolderPath()", true)]
    public static string UserSpeckleFolderPath => Path.Combine(UserApplicationDataPath, "Speckle");


    /// <summary>
    /// Returns the correct location of the AppData folder where Speckle is installed. Usually this would be the user's %appdata% folder, unless the install was made for all users.
    /// This folder contains Kits and othe data that can be shared among users of the same machine.
    /// </summary>
    /// <returns>The location of the AppData folder where Speckle is installed</returns>
    [Obsolete("Please use Helpers/SpecklePathProvider.InstallApplicationDataPath ", true)]
    public static string InstallApplicationDataPath =>

        Assembly.GetAssembly(typeof(Helpers)).Location.Contains("ProgramData")
          ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create)
          : UserApplicationDataPath;


    /// <summary>
    /// Envirenment Variable that allows to overwrite the <see cref="UserApplicationDataPath"/>
    /// /// </summary>
    private static string _speckleUserDataEnvVar = "SPECKLE_USERDATA_PATH";


    /// <summary>
    /// Returns the location of the User Application Data folder for the current roaming user, which contains user specific data such as accounts and cache.
    /// </summary>
    /// <returns>The location of the user's `%appdata%` folder.</returns>
    [Obsolete("Please use Helpers/SpecklePathProvider.UserApplicationDataPath", true)]
    public static string UserApplicationDataPath =>
      !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(_speckleUserDataEnvVar)) ?
      Environment.GetEnvironmentVariable(_speckleUserDataEnvVar) :
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);



  }
}
