namespace Speckle.Automate.Sdk.Schema.Triggers;

/// <summary>
/// Represents a single version creation trigger for the automation run.
/// </summary>
public class VersionCreationTrigger : AutomationRunTriggerBase
{
  public VersionCreationTriggerPayload Payload { get; set; }

  public VersionCreationTrigger(string modelId, string versionId)
  {
    TriggerType = "versionCreation";
    Payload = new VersionCreationTriggerPayload { ModelId = modelId, VersionId = versionId };
  }
}

/// <summary>
/// Represents the version creation trigger payload.
/// </summary>
public class VersionCreationTriggerPayload
{
  public string ModelId { get; set; }
  public string VersionId { get; set; }
}
