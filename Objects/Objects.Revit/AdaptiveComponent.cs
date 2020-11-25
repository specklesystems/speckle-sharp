using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class AdaptiveComponent : Base, IRevitElement
  {
    public string type { get; set; }

    [SchemaIgnore]
    public string family { get; set; }
    
    public List<Point> basePoints { get; set; }

    [SchemaOptional]
    public bool flipped { get; set; }
     
    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaIgnore]
    public Dictionary<string, object> typeParameters { get; set; }
  }
}
