using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Roof : Base
  {
    public ICurve outline { get; set; }

    [SchemaOptional]
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [SchemaDescription("Set in here any nested elements that this level might have.")]
    [SchemaOptional]
    public List<Base> elements { get; set; }

    public Roof() { }
  }
}

namespace Objects.BuiltElements.Revit
{
  [SchemaIgnore]
  public class RevitRoof : Roof
  {
    [SchemaOptional]
    public string family { get; set; }

    [SchemaOptional]
    public string type { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> parameters { get; set; }

    [SchemaOptional]
    public Dictionary<string, object> typeParameters { get; set; }

    [SchemaIgnore]
    public string elementId { get; set; }

    [SchemaOptional]
    public Level level { get; set; }

  }

  public class RevitExtrusionRoof : RevitRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }
  }

  public class RevitFootprintRoof : RevitRoof
  {
    public RevitLevel cutOffLevel { get; set; }
  }

}
