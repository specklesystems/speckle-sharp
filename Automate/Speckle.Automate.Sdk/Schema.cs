# nullable enable
namespace Speckle.Automate.Sdk;

///<summary>
///Values of the project, model and automation that triggere this function run.
///</summary>
public struct AutomationRunData
{
  public string ProjectId { get; set; }
  public string ModelId { get; set; }
  public string BranchName { get; set; }
  public string VersionId { get; set; }
  public string SpeckleServerUrl { get; set; }
  public string AutomationId { get; set; }
  public string AutomationRevisionId { get; set; }
  public string AutomationRunId { get; set; }
  public string FunctionId { get; set; }
  public string FunctionRelease { get; set; }
  public string FunctionName { get; set; }
}

struct ObjectResults
{
  public string Version => "1.0.0";
  public ObjectResultValues Values { get; set; }
}

struct ObjectResultValues
{
  public List<ResultCase> ObjectResults { get; set; }
  public List<string> BlobIds { get; set; }
}

///<summary>
/// Set the status of the automation.
///</summary>
public enum AutomationStatus
{
  Initializing,
  Running,
  Failed,
  Succeeded
}

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

public enum ObjectResultLevel
{
  Info,
  Warning,
  Error
}

public abstract class ObjectResultLevelMapping
{
  private const string Info = "INFO";
  private const string Warning = "WARNING";
  private const string Error = "ERROR";

  public static string Get(ObjectResultLevel level)
  {
    return level switch
    {
      ObjectResultLevel.Error => Error,
      ObjectResultLevel.Warning => Warning,
      ObjectResultLevel.Info => Info,
      _ => throw new ArgumentOutOfRangeException($"Not valid value for enum {level}")
    };
  }
}

public struct ResultCase
{
  public string Category { get; set; }
  public string Level { get; set; }
  public List<string> ObjectIds { get; set; }
  public string? Message { get; set; }
  public Dictionary<string, object>? Metadata { get; set; }
  public Dictionary<string, object>? VisualOverrides { get; set; }
}

public class AutomationResult
{
  public double Elapsed { get; set; }
  public string? ResultView { get; set; }
  public List<string> ResultVersions { get; set; } = new ();
  public List<string> Blobs { get; set; } = new();
  public string RunStatus { get; set; }
  public string? StatusMessage { get; set; }
  public List<ResultCase> ObjectResults { get; set; } = new();
}

public struct UploadResult
{
  public string BlobId { get; set; }
  public string FileName { get; set; }
  public int UploadStatus { get; set; }
}

public struct BlobUploadResponse
{
  public List<UploadResult> UploadResults { get; set; }
}
