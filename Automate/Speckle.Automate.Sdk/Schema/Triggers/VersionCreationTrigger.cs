namespace Speckle.Automate.Sdk.Schema.Triggers;

public class VersionCreationTrigger : AutomationRunTriggerBase
{
  public VersionCreationTriggerPayload Payload { get; set; }

  public VersionCreationTrigger(string modelId, string versionId)
  {
    TriggerType = "versionCreation";
    Payload = new VersionCreationTriggerPayload() { ModelId = modelId, VersionId = versionId };
  }
}

public class VersionCreationTriggerPayload
{
  public string ModelId { get; set; }
  public string VersionId { get; set; }
}
