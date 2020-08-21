using System;
using System.Reflection;
using DeviceId;
using Sentry;
using Sentry.Protocol;

namespace Speckle.Core.Logging
{
  public class Log
  {
    public static Log _instance;

    protected Log()
    {
      string deviceId = new DeviceIdBuilder()
        .AddMachineName()
        .AddProcessorId()
        .AddMotherboardSerialNumber()
        .ToString();

      //TODO: set DSN & env in CI/CD pipeline
      //TODO: turn Debug off
      SentrySdk.Init(o =>
        {
          o.Dsn = new Dsn("https://f29ec716d14d4121bb2a71c4f3ef7786@o436188.ingest.sentry.io/5396846");
          o.Environment = "dev";
          o.Debug = true;
          o.Release = "SpeckleCore@"+Assembly.GetExecutingAssembly().GetName().Version.ToString();  
        });

      SentrySdk.ConfigureScope(scope =>
      {
        scope.User = new User
        {
          Id = deviceId,
        };
      });
    }

    public static Log Instance()
    {
      if (_instance == null)
      {
        _instance = new Log();
      }

      return _instance;
    }

    /// <summary>
    /// Captures and throws an exception
    /// Unhandled exceptions are usually swallowed by host applications like Revit, Dynamo
    /// So they need to be sent manually.
    /// </summary>
    /// <param name="e">Exception to capture and throw</param>
    internal static void CaptureAndThrow(Exception e, SentryLevel level = SentryLevel.Error)
    {
      CaptureException(e, "core", level);
      throw e;
    }

    /// <summary>
    /// Captures and throws an exception
    /// Unhandled exceptions are usually swallowed by host applications like Revit, Dynamo
    /// So they need to be sent manually.
    /// </summary>
    /// <param name="e">Exception to capture and throw</param>
    /// <param name="e">Product where error is generated eg: core, connectorDynamo etc</param>
    public static void CaptureAndThrow(Exception e, string product, SentryLevel level = SentryLevel.Error)
    {
      CaptureException(e, product, level);
      throw e;
    }


    //capture and make sure Sentry is initialized
    public static void CaptureException(Exception e, string product = "core", SentryLevel level = SentryLevel.Error)
    {
      Instance();

      SentrySdk.WithScope(s =>
      {
        s.Level = level;
        s.SetTag("product", product);
        SentrySdk.CaptureException(e);
      });
    }
  }
}
