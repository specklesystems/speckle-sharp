using Speckle.Connectors.DUI.Models.Card.SendFilter;

namespace Speckle.Connectors.DUI.Models.Card;

public class SenderModelCard : ModelCard
{
  public ISendFilter? SendFilter { get; set; }

  // [JsonIgnore]
  // public HashSet<string> ChangedObjectIds { get; set; } = new();
}
