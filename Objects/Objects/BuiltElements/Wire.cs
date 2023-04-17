using System.Collections.Generic;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Wire : Base
  {
    public Wire() { }

    [SchemaInfo("Wire", "Creates a Speckle wire from curve segments and points", "BIM", "MEP")]
    public Wire(List<ICurve> segments)
    {
      this.segments = segments;
    }

    public List<ICurve> segments { get; set; }

    public string units { get; set; }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitWire : Wire
  {
    public RevitWire() { }

    [SchemaInfo("RevitWire", "Creates a Revit wire from points and level", "Revit", "MEP")]
    public RevitWire(
      List<double> constructionPoints,
      string family,
      string type,
      Level level,
      string wiringType = "Arc",
      List<Parameter> parameters = null
    )
    {
      this.constructionPoints = constructionPoints;
      this.family = family;
      this.type = type;
      this.level = level;
      this.wiringType = wiringType;
      this.parameters = parameters.ToBase();
    }

    public string family { get; set; }
    public string type { get; set; }
    public string wiringType { get; set; }
    public List<double> constructionPoints { get; set; } // used in constructor for revit native wires
    public string system { get; set; }
    public Level level { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
  }
}
