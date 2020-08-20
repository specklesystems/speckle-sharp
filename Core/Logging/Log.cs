using System;
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

      //TODO: set DSN & env in CI/CD pipeline, currently it's picked automatically from the SENTRY_DSN env variable
      //to set it on mac: https://stackoverflow.com/a/32405815/826060
      SentrySdk.Init(o =>
        {
          o.Environment = "dev";
        });

      SentrySdk.ConfigureScope(scope =>
      {
        scope.SetTag("project", "core");
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

    public static void CaptureAndThrow(Exception e)
    {
      var ex = new SpeckleException($"Dynamic object does not have the provided key.");
      //unhandled exceptions not caught when running in hosted applications, sending it explicitly
      Log.CaptureException(ex);
      throw ex;
    }


    //capture and make sure Sentry is initialized
    public static void CaptureException(Exception e)
    {
      Instance();
      SentrySdk.CaptureException(e);
    }
  }
}
