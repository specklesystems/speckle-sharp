using System.Collections.Generic;
using Speckle.Connectors.DUI.Utils;

namespace Speckle.Connectors.DUI.Models.Card.SendFilter;

public abstract class EverythingSendFilter : DiscriminatedObject, ISendFilter
{
  public string Name { get; set; } = "Everything";
  public string Summary { get; set; } = "All supported objects in the file.";
  public bool IsDefault { get; set; }
  public abstract List<string> GetObjectIds();
  public abstract bool CheckExpiry(string[] changedObjectIds);
}
