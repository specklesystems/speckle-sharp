using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Speckle.Core.Kits;

namespace Speckle.Core.Logging
{
  /// <summary>
  ///  Anonymous telemetry to help us understand how to make a better Speckle.
  ///  This really helps us to deliver a better open source project and product!
  /// </summary>
  public static class Setup
  {
    private readonly static string _suuidPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speckle", "suuid");

    static Setup()
    {
      //Set fallback values
      try
      {
        HostApplication = Process.GetCurrentProcess().ProcessName;
      }
      catch
      {
        HostApplication = "other (.NET)";
      }
    }
    
    public static void Init(string versionedHostApplication, string hostApplication)
    {
      HostApplication = hostApplication;
      VersionedHostApplication = versionedHostApplication;

      #if !NETSTANDARD1_5_OR_GREATER
      //needed by older .net frameworks, eg Revit 2019
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
      #endif


      Log.Initialize();
    }

    /// <summary>
    /// Set from the connectors, defines which current host application we're running on.
    /// </summary>
    internal static string HostApplication { get; private set; }
    /// <summary>
    /// Set from the connectors, defines which current host application we're running on - includes the version.
    /// </summary>
    internal static string VersionedHostApplication { get; private set; } = VersionedHostApplications.Other;

    private static string _suuid { get; set; }

    /// <summary>
    /// Tries to get the SUUID set by the Manager
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

          _suuid = "unknown-suuid";
        }
        return _suuid;
      }
    }
  }
}
