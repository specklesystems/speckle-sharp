using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;

namespace Speckle.Core.Logging;

/// <summary>
///  Anonymous telemetry to help us understand how to make a better Speckle.
///  This really helps us to deliver a better open source project and product!
/// </summary>
public static class Setup
{
  public static Mutex mutex;

  private static bool _initialized;

  [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
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

  /// <summary>
  /// Set from the connectors, defines which current host application we're running on.
  /// </summary>
  internal static string HostApplication { get; private set; }

  /// <summary>
  /// Set from the connectors, defines which current host application we're running on - includes the version.
  /// </summary>
  internal static string VersionedHostApplication { get; private set; } = HostApplications.Other.Slug;

  public static void Init(string versionedHostApplication, string hostApplication)
  {
    if (_initialized)
    {
      SpeckleLog.Logger
        .ForContext("newVersionedHostApplication", versionedHostApplication)
        .ForContext("newHostApplication", hostApplication)
        .Information(
          "Setup was already initialized with {currentHostApp} {currentVersionedHostApp}",
          hostApplication,
          versionedHostApplication
        );
      return;
    }

    _initialized = true;

    HostApplication = hostApplication;
    VersionedHostApplication = versionedHostApplication;

    //start mutex so that Manager can detect if this process is running
    mutex = new Mutex(false, "SpeckleConnector-" + hostApplication);

    SpeckleLog.Initialize(hostApplication, versionedHostApplication);

    foreach (var account in AccountManager.GetAccounts())
      Analytics.AddConnectorToProfile(account.GetHashedEmail(), hostApplication);
  }
}
