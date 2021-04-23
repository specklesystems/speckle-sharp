using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Wire : Base
  {
    public List<double> points { get; set; } // used for construction
    public List<ICurve> segments { get; set; }

    public Wire() { }

    [SchemaInfo("Wire", "Creates a Speckle wire from curve segments and points")]
    public Wire(List<ICurve> segments, List<double> points)
    {
      this.segments = segments;
      this.points = points;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitWire : Wire
  {
    public string family { get; set; }
    public string type { get; set; }
    public string wiringType { get; set; }
    public string system { get; set; }
    public Level level { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public RevitWire() { }

    [SchemaInfo("RevitWire", "Creates a Revit wire from points and level")]
    public RevitWire(List<double> points, string family, string type, Level level, List<Parameter> parameters = null)
    {
      this.points = points;
      this.family = family;
      this.type = type;
      this.level = level;
      this.parameters = parameters;
    }
  }
}
