namespace Speckle.Automate.Sdk.Schema;

public abstract class AutomationStatusMapping
{
  private const string Initializing = "INITIALIZING";
  private const string Running = "RUNNING";
  private const string Failed = "FAILED";
  private const string Succeeded = "SUCCEEDED";

  public static string Get(AutomationStatus status)
  {
    return status switch
    {
      AutomationStatus.Running => Running,
      AutomationStatus.Failed => Failed,
      AutomationStatus.Succeeded => Succeeded,
      AutomationStatus.Initializing => Initializing,
      _ => throw new ArgumentOutOfRangeException($"Not valid value for enum {status}")
    };
  }
}
