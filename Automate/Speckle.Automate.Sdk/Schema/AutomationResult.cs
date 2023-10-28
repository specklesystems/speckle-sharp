namespace Speckle.Automate.Sdk.Schema;

public class AutomationResult
{
  public double Elapsed { get; set; }
  public string? ResultView { get; set; }
  public List<string> ResultVersions { get; set; } = new();
  public List<string> Blobs { get; set; } = new();
  public string RunStatus { get; set; } = AutomationStatusMapping.Get(AutomationStatus.Running);
  public string? StatusMessage { get; set; }
  public List<ResultCase> ObjectResults { get; set; } = new();
}
