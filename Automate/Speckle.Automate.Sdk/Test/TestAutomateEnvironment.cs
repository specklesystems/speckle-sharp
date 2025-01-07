using System.Text.Json;
using Speckle.Core.Logging;

namespace Speckle.Automate.Sdk.Test;

public class TestAppSettings
{
  public string? SpeckleToken { get; set; }
  public string? SpeckleServerUrl { get; set; }
  public string? SpeckleProjectId { get; set; }
  public string? SpeckleAutomationId { get; set; }
}

public static class TestAutomateEnvironment
{
  public static TestAppSettings? AppSettings { get; private set; }

  private static string GetEnvironmentVariable(string environmentVariableName)
  {
    var value = TryGetEnvironmentVariable(environmentVariableName);

    if (value is null)
    {
      throw new SpeckleException($"Cannot run tests without a {environmentVariableName} environment variable");
    }

    return value;
  }

  private static string? TryGetEnvironmentVariable(string environmentVariableName)
  {
    return Environment.GetEnvironmentVariable(environmentVariableName);
  }

  private static TestAppSettings? GetAppSettings()
  {
    if (AppSettings != null)
    {
      return AppSettings;
    }

    var path = "./appsettings.json";
    var json = File.ReadAllText(path);

    var appSettings = JsonSerializer.Deserialize<TestAppSettings>(json);

    AppSettings = appSettings;

    return AppSettings;
  }

  public static string GetSpeckleToken()
  {
    return GetAppSettings()?.SpeckleToken ?? GetEnvironmentVariable("SPECKLE_TOKEN");
  }

  public static Uri GetSpeckleServerUrl()
  {
    var urlString =
      GetAppSettings()?.SpeckleServerUrl ?? TryGetEnvironmentVariable("SPECKLE_SERVER_URL") ?? "http://127.0.0.1:3000";

    return new Uri(urlString);
  }

  public static string GetSpeckleProjectId()
  {
    return GetAppSettings()?.SpeckleProjectId ?? GetEnvironmentVariable("SPECKLE_PROJECT_ID");
  }

  public static string GetSpeckleAutomationId()
  {
    return GetAppSettings()?.SpeckleAutomationId ?? GetEnvironmentVariable("SPECKLE_AUTOMATION_ID");
  }

  public static void Clear()
  {
    AppSettings = null;
  }
}
