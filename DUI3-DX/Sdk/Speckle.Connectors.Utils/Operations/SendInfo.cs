namespace Speckle.Connectors.Utils.Operations;

public readonly struct SendInfo
{
  public SendInfo(string accountId, string projectId, string modelId, string sourceApplication)
  {
    AccountId = accountId;
    ProjectId = projectId;
    ModelId = modelId;
    SourceApplication = sourceApplication;
  }

  public string AccountId { get; }
  public string ProjectId { get; }
  public string ModelId { get; }
  public string SourceApplication { get; }
}
