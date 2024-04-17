using System;
using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

[Obsolete("Class renamed to GisFeature")]
public class PolygonElement : Base
{
  [DetachProperty]
  public List<object> geometry { get; set; }
  public Base attributes { get; set; }
}
