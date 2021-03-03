using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sentry;
using Sentry.Protocol;

namespace Speckle.Core.Logging
{
  /// <summary>
  ///  Anonymous telemetry to help us understand how to make a better Speckle.
  ///  This really helps us to deliver a better open source project and product!
  /// </summary>
  public class Log
  {
    public static Log _instance;

    protected Log()
    {
      //TODO: set DSN & env in CI/CD pipeline
      SentrySdk.Init(o =>
      {
        o.Dsn = new Dsn("https://f29ec716d14d4121bb2a71c4f3ef7786@o436188.ingest.sentry.io/5396846");
        o.Environment = "dev";
        o.Debug = true;
        o.Release = "SpeckleCore@" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
      });

      SentrySdk.ConfigureScope(scope =>
      {
        scope.User = new User {Id = Setup.SUUID,};
        scope.SetTag("hostApplication", Setup.HostApplication);
      });
    }

    public static Log Instance()
    {
      if ( _instance == null )
      {
        _instance = new Log();
      }

      return _instance;
    }

    /// <summary>
    /// Captures and throws an exception
    /// Unhandled exceptions are usually swallowed by host applications like Revit, Dynamo so they need to be sent manually.
    /// If the exception is not a SpeckleException, is it wrapped in one and set as the InnerException
    /// </summary>
    /// <param name="e">Exception to capture and throw</param>
    public static void CaptureAndThrow(Exception e)
    {
      if ( !( e is TaskCanceledException ) )
        CaptureException(e);
      if (e is SpeckleException)
        throw e;
      throw new SpeckleException(e.Message, e, log: false);
    }

    //capture and make sure Sentry is initialized
    public static void CaptureException(
      Exception e,
      SentryLevel level = SentryLevel.Info,
      List<KeyValuePair<string, object>> extra = null)
    {
      Instance();

      SentrySdk.WithScope(s =>
      {
        s.Level = level;

        if ( extra != null )
          s.SetExtras(extra);
        if ( e is AggregateException aggregate )
          aggregate.InnerExceptions.ToList().ForEach(ex => SentrySdk.CaptureException(e));
        else
          SentrySdk.CaptureException(e);
      });
    }

    public static void AddBreadcrumb(string message)
    {
      Instance();
      SentrySdk.AddBreadcrumb(message);
    }
  }
}
