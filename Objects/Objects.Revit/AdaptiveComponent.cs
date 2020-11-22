using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class AdaptiveComponent : Element, IBuiltElement, IRevit
  {
    public string type { get; set; }
    public List<Point> basePoints { get; set; }

    [SchemaOptional]
    public bool flipped { get; set; }
    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }
    [SchemaIgnore]
    public string elementId { get; set; }

  }
}
