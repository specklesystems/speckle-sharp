#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Logging;

/// <summary>
///  Anonymous telemetry to help us understand how to make a better Speckle.
///  This really helps us to deliver a better open source project and product!
/// </summary>
public static class Analytics
{
  /// <summary>
  /// Default Mixpanel events
  /// </summary>
  public enum Events
  {
    /// <summary>
    /// Event triggered when data is sent to a Speckle Server
    /// </summary>
    Send,

    /// <summary>
    /// Event triggered when data is received from a Speckle Server
    /// </summary>
    Receive,

    /// <summary>
    /// Event triggered when a node is executed in a visual programming environment, it should contain the name of the action and the host application
    /// </summary>
    NodeRun,

    /// <summary>
    /// Event triggered when an action is executed in Desktop UI, it should contain the name of the action and the host application
    /// </summary>
    DUIAction,

    /// <summary>
    /// Event triggered when a node is first created in a visual programming environment, it should contain the name of the action and the host application
    /// </summary>
    NodeCreate,

    /// <summary>
    /// Event triggered when the import/export alert is launched or closed
    /// </summary>
    ImportExportAlert,

    /// <summary>
    /// Event triggered when the connector is registered
    /// </summary>
    Registered,

    /// <summary>
    /// Event triggered by the Mapping Tool
    /// </summary>
    MappingsAction,

    /// <summary>
    /// Event triggered when user selects object to convert to Speckle on Send
    /// </summary>
    ConvertToSpeckle,

    /// <summary>
    /// Event triggered when user selects object to convert to Native on Receive
    /// </summary>
    ConvertToNative
  }

  private const string MIXPANEL_TOKEN = "acd87c5a50b56df91a795e999812a3a4";
  private const string MIXPANEL_SERVER = "https://analytics.speckle.systems";

  /// <summary>
  /// Cached email
  /// </summary>
  private static string LastEmail { get; set; }

  /// <summary>
  /// Cached server URL
  /// </summary>
  private static string LastServer { get; set; }

  /// <summary>
  /// <see langword="false"/> when the DEBUG pre-processor directive is <see langword="true"/>, <see langword="false"/> otherwise
  /// </summary>
  /// <remarks>This must be kept as a computed property, not a compile time const</remarks>
  internal static bool IsReleaseMode =>
#if DEBUG
    false;
#else
    true;
#endif

  /// <summary>
  /// Tracks an event without specifying the email and server.
  /// It's not always possible to know which account the user has selected, especially in visual programming.
  /// Therefore we are caching the email and server values so that they can be used also when nodes such as "Serialize" are used.
  /// If no account info is cached, we use the default account data.
  /// </summary>
  /// <param name="eventName">Name of the even</param>
  /// <param name="customProperties">Additional parameters to pass in to event</param>
  /// <param name="isAction">True if it's an action performed by a logged user</param>
  public static void TrackEvent(
    Events eventName,
    Dictionary<string, object> customProperties = null,
    bool isAction = true
  )
  {
    string email;
    string server;

    if (LastEmail != null && LastServer != null && LastServer != "no-account-server")
    {
      email = LastEmail;
      server = LastServer;
    }
    else
    {
      var acc = AccountManager.GetDefaultAccount();
      if (acc == null)
      {
        var macAddr = NetworkInterface
          .GetAllNetworkInterfaces()
          .Where(
            nic =>
              nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
          )
          .Select(nic => nic.GetPhysicalAddress().ToString())
          .FirstOrDefault();

        email = macAddr;
        server = "no-account-server";
        isAction = false;
      }
      else
      {
        email = acc.GetHashedEmail();
        server = acc.GetHashedServer();
      }
    }

    TrackEvent(email, server, eventName, customProperties, isAction);
  }

  /// <summary>
  /// Tracks an event from a specified account, anonymizes personal information
  /// </summary>
  /// <param name="account">Account to use, it will be anonymized</param>
  /// <param name="eventName">Name of the event</param>
  /// <param name="customProperties">Additional parameters to pass to the event</param>
  /// <param name="isAction">True if it's an action performed by a logged user</param>
  public static void TrackEvent(
    Account account,
    Events eventName,
    Dictionary<string, object> customProperties = null,
    bool isAction = true
  )
  {
    if (account == null)
    {
      TrackEvent(eventName, customProperties, isAction);
    }
    else
    {
      TrackEvent(account.GetHashedEmail(), account.GetHashedServer(), eventName, customProperties, isAction);
    }
  }

  /// <summary>
  /// Tracks an event from a specified email and server, anonymizes personal information
  /// </summary>
  /// <param name="hashedEmail">Email of the user anonymized</param>
  /// <param name="hashedServer">Server URL anonymized</param>
  /// <param name="eventName">Name of the event</param>
  /// <param name="customProperties">Additional parameters to pass to the event</param>
  /// <param name="isAction">True if it's an action performed by a logged user</param>
  private static void TrackEvent(
    string hashedEmail,
    string hashedServer,
    Events eventName,
    Dictionary<string, object> customProperties = null,
    bool isAction = true
  )
  {
    LastEmail = hashedEmail;
    LastServer = hashedServer;

    if (!IsReleaseMode)
    {
      //only track in prod
      return;
    }

    Task.Run(async () =>
    {
      try
      {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var properties = new Dictionary<string, object>
        {
          { "distinct_id", hashedEmail },
          { "server_id", hashedServer },
          { "token", MIXPANEL_TOKEN },
          { "hostApp", Setup.HostApplication },
          { "hostAppVersion", Setup.VersionedHostApplication },
          {
            "core_version",
            FileVersionInfo.GetVersionInfo(executingAssembly.Location).ProductVersion
              ?? executingAssembly.GetName().Version.ToString()
          },
          { "$os", GetOs() }
        };

        if (isAction)
        {
          properties.Add("type", "action");
        }

        if (customProperties != null)
        {
          foreach (KeyValuePair<string, object> customProp in customProperties)
          {
            properties[customProp.Key] = customProp.Value;
          }
        }

        string json = JsonConvert.SerializeObject(new { @event = eventName.ToString(), properties });

        var query = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("data=" + HttpUtility.UrlEncode(json))));

        using HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        query.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var res = await client.PostAsync(MIXPANEL_SERVER + "/track?ip=1", query).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger
          .ForContext("eventName", eventName.ToString())
          .ForContext("isAction", isAction)
          .Warning(ex, "Analytics event failed {exceptionMessage}", ex.Message);
      }
    });
  }

  internal static void AddConnectorToProfile(string hashedEmail, string connector)
  {
    Task.Run(async () =>
    {
      try
      {
        var data = new Dictionary<string, object>
        {
          { "$token", MIXPANEL_TOKEN },
          { "$distinct_id", hashedEmail },
          {
            "$union",
            new Dictionary<string, object>
            {
              {
                "Connectors",
                new List<string> { connector }
              }
            }
          }
        };
        string json = JsonConvert.SerializeObject(data);

        var query = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("data=" + HttpUtility.UrlEncode(json))));
        using HttpClient client = Http.GetHttpProxyClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        query.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var res = await client.PostAsync(MIXPANEL_SERVER + "/engage#profile-union", query).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.ForContext("connector", connector).Warning(ex, "Failed add connector to profile");
      }
    });
  }

  internal static void IdentifyProfile(string hashedEmail, string connector)
  {
    Task.Run(async () =>
    {
      try
      {
        var data = new Dictionary<string, object>
        {
          { "$token", MIXPANEL_TOKEN },
          { "$distinct_id", hashedEmail },
          {
            "$set",
            new Dictionary<string, object> { { "Identified", true } }
          }
        };
        string json = JsonConvert.SerializeObject(data);

        var query = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("data=" + HttpUtility.UrlEncode(json))));
        using HttpClient client = Http.GetHttpProxyClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        query.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var res = await client.PostAsync(MIXPANEL_SERVER + "/engage#profile-set", query).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.ForContext("connector", connector).Warning(ex, "Failed identify profile");
      }
    });
  }

  private static string GetOs()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      return "Windows";
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      return "Mac OS X";
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      return "Linux";
    }

    return "Unknown";
  }
}
