using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DeviceId;

namespace Speckle.Core.Logging
{
  public static class Setup
  {
    private readonly static string _suuidPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speckle", "suuid");
    public static void Init(string hostApplication)
    {
      HostApplication = hostApplication;

      Log.Instance();
      Tracker.TrackEvent(Tracker.SESSION_START);
    }

    //Dynamo / Grasshopper etc....
    internal static string HostApplication { get; private set; } = "unknown";

    private static string _suuid { get; set; }

    /// <summary>
    /// Tries to get the SUUID set by the Manager, and if can't, it falls back to a DeviceID
    /// </summary>
    internal static string SUUID
    {
      get
      {
        if (_suuid == null)
        {
          try
          {
            _suuid = File.ReadAllText(_suuidPath);
            if (!string.IsNullOrEmpty(_suuid))
              return _suuid;
          }
          catch { }

          _suuid = new DeviceIdBuilder()
            .AddMachineName()
            .AddProcessorId()
            .AddMotherboardSerialNumber()
            .ToString();
        }
        return _suuid;
      }
    }
  }
}
