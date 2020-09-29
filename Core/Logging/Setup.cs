using System;
using System.Collections.Generic;
using System.Text;
using DeviceId;

namespace Speckle.Core.Logging
{
  public static class Setup
  {
    public static void Init(string hostApplication)
    {
      HostApplication = hostApplication;

      Log.Instance();
      Tracker.TrackEvent(Tracker.SESSION_START);
    }

    //Dynamo / Grasshopper etc....
    internal static string HostApplication { get; private set; } = "unknown";

    private static string _deviceId { get; set; }
    internal static string DeviceID
    {
      get
      {
        if (_deviceId == null)
        {
          _deviceId = new DeviceIdBuilder()
            .AddMachineName()
            .AddProcessorId()
            .AddMotherboardSerialNumber()
            .ToString();
        }
        return _deviceId;
      }
    }
  }
}
