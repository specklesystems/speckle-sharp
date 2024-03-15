namespace Speckle.Connectors.DUI.Objects;

public interface ISpeckleHostObject
{
  public string ApplicationId { get; }
  public string SpeckleId { get; }
  public bool IsExpired { get; }
}
