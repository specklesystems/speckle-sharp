#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Sentry;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Exceptions;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;

namespace Speckle.Core.Logging
{
  /// <summary>
  /// Configuration object for the Speckle logging system.
  /// </summary>
  public class SpeckleLogConfiguration
  {
    /// <summary>
    /// Log events bellow this level are silently dropped
    /// </summary>
    public LogEventLevel minimumLevel;

    /// <summary>
    /// Flag to enable console sink
    /// </summary>
    public bool logToConsole;

    /// <summary>
    /// Flag to enable Seq sink
    /// </summary>
    public bool logToSeq;

    /// <summary>
    /// Flag to enable Sentry sink
    /// </summary>
    public bool logToSentry;

    /// <summary>
    /// Flag to override the default Sentry DNS
    /// </summary>
    public string sentryDns = "https://f29ec716d14d4121bb2a71c4f3ef7786@o436188.ingest.sentry.io/5396846";

    /// <summary>
    /// Flag to enable File sink
    /// </summary>
    public bool logToFile;

    /// <summary>
    /// Flag to enable enhanced log context. This adds the following enrich calls:
    /// - WithClientAgent
    /// - WithClientIp
    /// - WithExceptionDetails
    /// </summary>
    public bool enhancedLogContext;

    /// <summary>
    /// Default SpeckleLogConfiguration constructor.
    /// These are the sane defaults we should be using across connectors.
    /// </summary>
    /// <param name="minimumLevel">Log events bellow this level are silently dropped</param>
    /// <param name="logToConsole">Flag to enable console log sink</param>
    /// <param name="logToSeq">Flag to enable Seq log sink</param>
    /// <param name="logToSentry">Flag to enable Sentry log sink</param>
    /// <param name="logToFile">Flag to enable File log sink</param>
    /// <param name="enhancedLogContext">Flag to enable enhanced context on every log event</param>
    public SpeckleLogConfiguration(
      LogEventLevel minimumLevel = LogEventLevel.Debug,
      bool logToConsole = true,
      bool logToSeq = true,
      bool logToSentry = true,
      bool logToFile = true,
      bool enhancedLogContext = true
    )
    {
      this.minimumLevel = minimumLevel;
      this.logToConsole = logToConsole;
      this.logToSeq = logToSeq;
      this.logToSentry = logToSentry;
      this.logToFile = logToFile;
      this.enhancedLogContext = enhancedLogContext;
    }
  }

  /// <summary>
  /// Configurator class for a standardized logging system across Speckle (sharp).
  /// </summary>
  public static class SpeckleLog
  {
    private static ILogger? _logger;

    public static ILogger Logger => _logger ?? throw new SpeckleException(
      $"The logger has not been initialized. Please call {typeof(SpeckleLog).FullName}.{nameof(Initialize)}");
    private static bool _initialized = false;

    /// <summary>
    /// Initialize logger configuration for a global Serilog.Log logger.
    /// </summary>
    public static void Initialize(
      string hostApplicationName,
      string? hostApplicationVersion,
      SpeckleLogConfiguration? logConfiguration = null
    )
    {
      if (_initialized)
        return;

      logConfiguration ??= new SpeckleLogConfiguration();

      _logger = CreateConfiguredLogger(
        hostApplicationName,
        hostApplicationVersion,
        logConfiguration
      );
      Log.Logger = Logger;

      _addUserIdToGlobalContextFromDefaultAccount();
      _addVersionInfoToGlobalContext();
      _addHostOsInfoToGlobalContext();
      _addHostApplicationDataToGlobalContext(hostApplicationName, hostApplicationVersion);

      Logger.ForContext("userApplicationDataPath", SpecklePathProvider.UserApplicationDataPath())
        .ForContext("installApplicationDataPath", SpecklePathProvider.InstallApplicationDataPath)
        .ForContext("speckleLogConfiguration", logConfiguration)
        .Information(
          "Initialized logger inside {hostApplication}/{productVersion}/{version} for user {id}. Path info {userApplicationDataPath} {installApplicationDataPath}."
        );

      _initialized = true;
    }

    /// <summary>
    /// Create a new fully configured Logger instance.
    /// </summary>
    /// <param name="hostApplicationName">Name of the application using this SDK ie.: "Rhino"</param>
    /// <param name="hostApplicationVersion">Public version slug of the application using this SDK ie.: "2023"</param>
    /// <param name="logConfiguration">Input configuration object.</param>
    /// <returns>Logger instance</returns>
    public static Serilog.Core.Logger CreateConfiguredLogger(
      string hostApplicationName,
      string? hostApplicationVersion,
      SpeckleLogConfiguration logConfiguration
    )
    {
      // TODO: check if we have write permissions to the file.
      // if not, disable file sink, even if its enabled in the config
      // show a warning about that...
      var canLogToFile = true;
      var logFilePath = Path.Combine(
        SpecklePathProvider.LogFolderPath(hostApplicationName, hostApplicationVersion),
        "SpeckleCoreLog.txt"
      );
      var serilogLogConfiguration = new LoggerConfiguration().MinimumLevel
        .Is(logConfiguration.minimumLevel)
        .Enrich.FromLogContext()
        .Enrich.FromGlobalLogContext();

      if (logConfiguration.enhancedLogContext)
        serilogLogConfiguration = serilogLogConfiguration.Enrich.WithClientAgent()
                               .Enrich.WithClientIp()
                               .Enrich.WithExceptionDetails();


      if (logConfiguration.logToFile && canLogToFile)
        serilogLogConfiguration = serilogLogConfiguration.WriteTo.File(
          logFilePath,
          rollingInterval: RollingInterval.Day,
          retainedFileCountLimit: 10
        );

      if (logConfiguration.logToConsole)
        serilogLogConfiguration = serilogLogConfiguration.WriteTo.Console();

      if (logConfiguration.logToSeq)
        serilogLogConfiguration = serilogLogConfiguration.WriteTo.Seq(
          "https://seq.speckle.systems",
          apiKey: "agZqxG4jQELxQQXh0iZQ"
        );

      if (logConfiguration.logToSentry)
      {
        var env = "production";

#if DEBUG
        env = "dev";
#endif

        serilogLogConfiguration = serilogLogConfiguration.WriteTo.Sentry(o =>
        {
          o.Dsn = logConfiguration.sentryDns;
          o.Debug = false;
          o.Environment = env;
          // Set traces_sample_rate to 1.0 to capture 100% of transactions for performance monitoring.
          // We recommend adjusting this value in production.
          o.TracesSampleRate = 1.0;
          // Enable Global Mode if running in a client app
          o.IsGlobalModeEnabled = true;
          // Debug and higher are stored as breadcrumbs (default is Information)
          o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
          // Warning and higher is sent as event (default is Error)
          o.MinimumEventLevel = LogEventLevel.Error;
        });
      }


      var logger = serilogLogConfiguration.CreateLogger();
      if (logConfiguration.logToFile && !canLogToFile)
        logger.Warning("Log to file is enabled, but cannot write to {LogFilePath}", logFilePath);
      return logger;
    }

    private static void _addUserIdToGlobalContextFromDefaultAccount()
    {
      var machineName = Environment.MachineName;
      var userName = Environment.UserName;
      var id = Crypt.Hash($"{machineName}:{userName}");
      try
      {
        var defaultAccount = AccountManager.GetDefaultAccount();
        if (defaultAccount != null)
          id = defaultAccount.GetHashedEmail();
      }
      catch (Exception ex)
      {
        Logger.Warning(ex, "Cannot set user id for the global log context.");
      }
      GlobalLogContext.PushProperty("id", id);

      SentrySdk.ConfigureScope(scope =>
      {
        scope.User = new User { Id = id, };
      });
    }

    private static void _addVersionInfoToGlobalContext()
    {
      var assembly = Assembly.GetExecutingAssembly().Location;
      var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly);

      GlobalLogContext.PushProperty("version", fileVersionInfo.FileVersion);
      GlobalLogContext.PushProperty("productVersion", fileVersionInfo.ProductVersion);
    }

    private static string _deterimineHostOsSlug()
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "MacOS";
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
      return RuntimeInformation.OSDescription;
    }
    private static void _addHostOsInfoToGlobalContext()
    {

      var osVersion = Environment.OSVersion;
      var osArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
      GlobalLogContext.PushProperty("hostOs", _deterimineHostOsSlug());
      GlobalLogContext.PushProperty("hostOsVersion", osVersion);
      GlobalLogContext.PushProperty("hostOsArchitecture", osArchitecture);
    }

    private static void _addHostApplicationDataToGlobalContext(
      string hostApplicationName,
      string? hostApplicationVersion
    )
    {
      GlobalLogContext.PushProperty(
        "hostApplication",
        $"{hostApplicationName}{hostApplicationVersion ?? ""}"
      );

      SentrySdk.ConfigureScope(scope =>
      {
        scope.SetTag("hostApplication", hostApplicationName);
      });
    }
  }
}
