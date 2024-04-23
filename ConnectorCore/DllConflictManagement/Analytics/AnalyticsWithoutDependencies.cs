using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using Speckle.DllConflictManagement.EventEmitter;
using Speckle.DllConflictManagement.Serialization;

namespace Speckle.DllConflictManagement.Analytics;

/// <summary>
///  A version of the Analytics class in Core that doesn't have any dependencies. This class will load and subscribe
///  to the eventEmitter's Action event, but will hopefully get unsubscribed and replaced by the full version in Core
/// </summary>
public sealed class AnalyticsWithoutDependencies
{
  private const string MIXPANEL_TOKEN = "acd87c5a50b56df91a795e999812a3a4";
  private const string MIXPANEL_SERVER = "https://analytics.speckle.systems";
  private readonly ISerializer _serializer;
  private readonly string _hostApplication;
  private readonly string _hostApplicationVersion;
  private readonly DllConflictEventEmitter _eventEmitter;

  public AnalyticsWithoutDependencies(
    DllConflictEventEmitter eventEmitter,
    ISerializer serializer,
    string hostApplication,
    string hostApplicationVersion
  )
  {
    _eventEmitter = eventEmitter;
    _serializer = serializer;
    _hostApplication = hostApplication;
    _hostApplicationVersion = hostApplicationVersion;
  }

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
  public void TrackEvent(Events eventName, Dictionary<string, object?>? customProperties = null, bool isAction = true)
  {
    Task.Run(async () => await TrackEventAsync(eventName, customProperties, isAction).ConfigureAwait(false));
  }

  /// <summary>
  /// Tracks an event from a specified email and server, anonymizes personal information
  /// </summary>
  /// <param name="eventName">Name of the event</param>
  /// <param name="customProperties">Additional parameters to pass to the event</param>
  /// <param name="isAction">True if it's an action performed by a logged user</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Catching all exceptions to avoid an unobserved exception that could crash the host app"
  )]
  private async Task TrackEventAsync(
    Events eventName,
    Dictionary<string, object?>? customProperties = null,
    bool isAction = true
  )
  {
    if (!IsReleaseMode)
    {
      //only track in prod
      return;
    }

    try
    {
      var executingAssembly = Assembly.GetExecutingAssembly();
      var properties = new Dictionary<string, object?>
      {
        { "distinct_id", "undefined" },
        { "server_id", "no-account-server" },
        { "token", MIXPANEL_TOKEN },
        { "hostApp", _hostApplication },
        { "hostAppVersion", _hostApplicationVersion },
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
        foreach (KeyValuePair<string, object?> customProp in customProperties)
        {
          properties[customProp.Key] = customProp.Value;
        }
      }

      string json = _serializer.Serialize(new { @event = eventName.ToString(), properties });

      var query = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("data=" + HttpUtility.UrlEncode(json))));

      using HttpClient client = new();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
      query.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      var res = await client.PostAsync(MIXPANEL_SERVER + "/track?ip=1", query).ConfigureAwait(false);
      res.EnsureSuccessStatusCode();
    }
    catch (Exception ex)
    {
      _eventEmitter.EmitError(
        new LoggingEventArgs(
          $"An exception was thrown in class {nameof(AnalyticsWithoutDependencies)} while attempting to record analytics",
          ex
        )
      );
    }
  }

  public void TrackEvent(object sender, ActionEventArgs args)
  {
    _ = Enum.TryParse(args.EventName, out Analytics.Events eventName);
    TrackEvent(eventName, args.EventProperties);
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
