using System;
using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

[Obsolete("PolygonElement was replaced by a more generic class, \"GisFeature\", which contains more information")]
public class PolygonElement : Base
{
  [DetachProperty]
  public List<object> geometry { get; set; }
  public Base attributes { get; set; }
}
