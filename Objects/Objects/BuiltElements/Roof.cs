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
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [DetachProperty]
    public List<Base> elements { get; set; }

    public Roof() { }

    [SchemaInfo("Roof", "Creates a Speckle roof")]
    public Roof(ICurve outline, List<ICurve> voids = null, List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }
  }
}

namespace Objects.BuiltElements.Revit.RevitRoof
{
  public class RevitRoof : Roof
  {
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitRoof()
    {
    }
  }

  public class RevitExtrusionRoof : RevitRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }

    public RevitExtrusionRoof()
    {

    }

    [SchemaInfo("RevitExtrusionRoof", "Creates a Revit roof by extruding a curve")]
    public RevitExtrusionRoof(string family, string type, double start, double end, Line referenceLine, Level level,
      List<Base> elements = null,
      Dictionary<string, object> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.parameters = parameters;
      this.level = level;
      this.start = start;
      this.end = end;
      this.referenceLine = referenceLine;
      this.elements = elements;
    }

  }

  public class RevitFootprintRoof : RevitRoof
  {
    public RevitLevel cutOffLevel { get; set; }

    public RevitFootprintRoof()
    {

    }
    [SchemaInfo("RevitFootprintRoof", "Creates a Revit roof by outline")]
    public RevitFootprintRoof(ICurve outline, string family, string type, Level level, RevitLevel cutOffLevel = null, List<ICurve> voids = null,
      List<Base> elements = null,
      Dictionary<string, object> parameters = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.family = family;
      this.type = type;
      this.parameters = parameters;
      this.level = level;
      this.cutOffLevel = cutOffLevel;
      this.elements = elements;
    }
  }

}
