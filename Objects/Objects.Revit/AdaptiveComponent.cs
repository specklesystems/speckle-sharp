using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class AdaptiveComponent : Element, IRevitElement
  {
    public bool flipped { get; set; }
    public List<Point> basePoints { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }

    public RevitLevel level { get; set; }

    [SchemaBuilderIgnore]
    public string elementId { get; set; }
  }
}
