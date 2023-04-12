using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Credentials;

namespace Speckle.Core.Logging;

/// <summary>
///  Anonymous telemetry to help us understand how to make a better Speckle.
///  This really helps us to deliver a better open source project and product!
/// </summary>
public static class OldLog
{
  private static bool _initialized = false;

  /// <summary>
  /// Initializes Sentry
  /// </summary>
  public static void Initialize()
  {
    try
    {
      if (_initialized)
        return;

      var dsn = "https://f29ec716d14d4121bb2a71c4f3ef7786@o436188.ingest.sentry.io/5396846";
      var env = "production";
      var debug = false;
#if DEBUG
      env = "dev";
      dsn = null;
      debug = true;
#endif

      SentrySdk.Init(o =>
      {
        o.Dsn = dsn;
        o.Environment = env;
        o.Debug = debug;
        o.Release = "SpeckleCore@" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        o.StackTraceMode = StackTraceMode.Enhanced;
        o.AttachStacktrace = true;
        o.AddExceptionFilterForType<HttpRequestException>();
        o.AddExceptionFilterForType<TaskCanceledException>();
      });

      var id = "unknown";

      try
      {
        var da = AccountManager.GetDefaultAccount();
        if (da != null) id = da.GetHashedEmail();
      }
      catch (Exception ex) { }

      SentrySdk.ConfigureScope(scope =>
      {
        scope.User = new User { Id = id };
        scope.SetTag("hostApplication", Setup.HostApplication);
      });

      _initialized = true;
    }
    catch (Exception ex)
    {
      //swallow
    }
  }

  /// <summary>
  /// Captures an Exception and makes sure Sentry is initialized
  /// </summary>
  /// <param name="e"></param>
  /// <param name="level"></param>
  /// <param name="extra"></param>
  public static void CaptureException(
    Exception e,
    SentryLevel level = SentryLevel.Info,
    List<KeyValuePair<string, object>> extra = null
  )
  {
    Initialize();

    //ignore infos as they're hogging us
    if (level == SentryLevel.Info)
      return;

    SentrySdk.CaptureException(
      e,
      scope =>
      {
        scope.Level = level;
        if (extra != null)
          scope.SetExtras(extra);
      }
    );
  }

  /// <summary>
  /// Adds a Breadcrumb and makes sure Sentry is initialized
  /// </summary>
  /// <param name="message"></param>
  public static void AddBreadcrumb(string message)
  {
    Initialize();
    SentrySdk.AddBreadcrumb(message);
  }
}