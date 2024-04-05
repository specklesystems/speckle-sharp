using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class GisFeature : Base
{
  public GisFeature() { }

  public GisFeature(List<Base> geometry, Base attributes)
  {
    this.geometry = geometry;
    this.attributes = attributes;
  }

  public GisFeature(List<Base> geometry, Base attributes, List<Base> displayValue)
  {
    this.geometry = geometry;
    this.attributes = attributes;
    this.displayValue = displayValue;
  }

  [DetachProperty]
  public List<Base>? geometry { get; set; }

  [DetachProperty]
  public List<Base>? displayValue { get; set; }
  public Base? attributes { get; set; }
}
