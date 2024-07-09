using Speckle.Automate.Sdk.Schema.Triggers;

namespace Speckle.Automate.Sdk.Schema;

///<summary>
/// Values of the project, model and automation that triggered this function run.
///</summary>
public struct AutomationRunData
{
  public string ProjectId { get; set; }
  public string SpeckleServerUrl { get; set; }
  public string AutomationId { get; set; }
  public string AutomationRunId { get; set; }
  public string FunctionRunId { get; set; }
  public List<VersionCreationTrigger> Triggers { get; set; }
}
