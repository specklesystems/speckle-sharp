using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;

namespace Speckle.Core.Logging
{
  /// <summary>
  ///  Anonymous telemetry to help us understand how to make a better Speckle.
  ///  This really helps us to deliver a better open source project and product!
  /// </summary>
  public static class Setup
  {
    public static Mutex mutex;

    private static bool initialized = false;

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
      if (initialized)
        return;

      initialized = true;

      HostApplication = hostApplication;
      VersionedHostApplication = versionedHostApplication;

      //start mutex so that Manager can detect if this process is running
      mutex = new Mutex(false, "SpeckleConnector-" + hostApplication);

#if !NETSTANDARD1_5_OR_GREATER
      //needed by older .net frameworks, eg Revit 2019
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif


      SpeckleLog.Initialize(hostApplication, versionedHostApplication);

      foreach (var account in AccountManager.GetAccounts())
        Analytics.AddConnectorToProfile(account.GetHashedEmail(), hostApplication);
    }

    /// <summary>
    /// Set from the connectors, defines which current host application we're running on.
    /// </summary>
    internal static string HostApplication { get; private set; }
    /// <summary>
    /// Set from the connectors, defines which current host application we're running on - includes the version.
    /// </summary>
    internal static string VersionedHostApplication { get; private set; } = HostApplications.Other.Slug;


  }
}
