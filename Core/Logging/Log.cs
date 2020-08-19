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


    //capture and make sure Sentry is initialized
    public void CaptureException(Exception e)
    {
      SentrySdk.CaptureException(e);
    }
  }
}
