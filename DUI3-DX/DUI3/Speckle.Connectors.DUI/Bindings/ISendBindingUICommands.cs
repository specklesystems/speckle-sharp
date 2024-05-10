namespace Speckle.Connectors.DUI.Bindings;

public interface ISendBindingUICommands
{
  public void RefreshSendFilters(string frontEndName);

  public void SetModelsExpired(string frontEndName, IEnumerable<string> expiredModelIds);

  public void SetModelCreatedVersionId(string frontEndName, string modelCardId, string versionId);
}
