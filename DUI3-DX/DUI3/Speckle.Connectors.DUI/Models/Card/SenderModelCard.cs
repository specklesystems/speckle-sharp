using System.Collections.Generic;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.DUI.Models.Card;

public class SenderModelCard : ModelCard
{
  public ISendFilter SendFilter { get; set; }

  [JsonIgnore]
  public HashSet<string> ChangedObjectIds { get; set; } = new();
}
