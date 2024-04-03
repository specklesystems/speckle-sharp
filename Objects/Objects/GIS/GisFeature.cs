using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
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

  [DetachProperty]
  public List<Base>? geometry { get; set; }

  [DetachProperty]
  public List<Base>? displayValue { get; set; }
  public Base? attributes { get; set; }
}
