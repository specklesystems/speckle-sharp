using Speckle.Connectors.DUI.Utils;

namespace Speckle.Connectors.DUI.Models.Card.SendFilter;

public abstract class DirectSelectionSendFilter : DiscriminatedObject, ISendFilter
{
  public string Name { get; set; } = "Selection";
  public string Summary { get; set; }
  public bool IsDefault { get; set; }
  public List<string> SelectedObjectIds { get; set; } = new List<string>();
  public abstract List<string> GetObjectIds();
  public abstract bool CheckExpiry(string[] changedObjectIds);
}
