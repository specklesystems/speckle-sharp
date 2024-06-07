namespace Speckle.Automate.Sdk.Schema;

public abstract class AutomationStatusMapping
{
  private const string INITIALIZING = "INITIALIZING";
  private const string RUNNING = "RUNNING";
  private const string FAILED = "FAILED";
  private const string SUCCEEDED = "SUCCEEDED";
  private const string EXCEPTION = "EXCEPTION";

  public static string Get(AutomationStatus status) =>
    status switch
    {
      AutomationStatus.Running => RUNNING,
      AutomationStatus.Failed => FAILED,
      AutomationStatus.Succeeded => SUCCEEDED,
      AutomationStatus.Initializing => INITIALIZING,
      AutomationStatus.Exception => EXCEPTION,
      _ => throw new ArgumentOutOfRangeException($"Not valid value for enum {status}")
    };
}
