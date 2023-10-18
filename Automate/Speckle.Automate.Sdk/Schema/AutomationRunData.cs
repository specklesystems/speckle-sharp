# nullable enable
namespace Speckle.Automate.Sdk.Schema;

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
