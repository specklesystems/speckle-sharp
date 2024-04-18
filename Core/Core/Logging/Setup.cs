#nullable disable
using System;
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
[SuppressMessage(
  "Naming",
  "CA1708:Identifiers should differ by more than case",
  Justification = "Class contains obsolete members that are kept for backwards compatiblity"
)]
public static class Setup
{
  public static Mutex Mutex { get; set; }

  private static bool s_initialized;

  static Setup()
  {
    //Set fallback values
    try
    {
      HostApplication = Process.GetCurrentProcess().ProcessName;
    }
    catch (InvalidOperationException)
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

  public static void Init(
    string versionedHostApplication,
    string hostApplication,
    SpeckleLogConfiguration logConfiguration = null
  )
  {
    if (s_initialized)
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

    s_initialized = true;

    HostApplication = hostApplication;
    VersionedHostApplication = versionedHostApplication;

    //start mutex so that Manager can detect if this process is running
    Mutex = new Mutex(false, "SpeckleConnector-" + hostApplication);

    SpeckleLog.Initialize(hostApplication, versionedHostApplication, logConfiguration);

    foreach (var account in AccountManager.GetAccounts())
    {
      Analytics.AddConnectorToProfile(account.GetHashedEmail(), hostApplication);
      Analytics.IdentifyProfile(account.GetHashedEmail(), hostApplication);
    }
  }

  [Obsolete("Use " + nameof(Mutex))]
  [SuppressMessage("Style", "IDE1006:Naming Styles")]
  public static Mutex mutex
  {
    get => Mutex;
    set => Mutex = value;
  }
}
