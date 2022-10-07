using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Roof : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [DetachProperty]
    public List<Base> elements { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    
    public string units { get; set; }

    public Roof() { }
    
    [SchemaDeprecated]
    [SchemaInfo("Roof", "Creates a Speckle roof", "BIM", "Architecture")]
    public Roof([SchemaMainParam] ICurve outline, List<ICurve> voids = null, List<Base> elements = null)
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
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitRoof() { }
  }

  public class RevitExtrusionRoof : RevitRoof
  {
    public double start { get; set; }
    public double end { get; set; }
    public Line referenceLine { get; set; }

    public RevitExtrusionRoof() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit extrusion roof
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="referenceLine"></param>
    /// <param name="level"></param>
    /// <param name="elements"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="start"/> and <paramref name="end"/> params</remarks>
    [SchemaInfo("RevitExtrusionRoof", "Creates a Revit roof by extruding a curve", "Revit", "Architecture")]
    public RevitExtrusionRoof(string family, string type,
      [SchemaParamInfo("Extrusion start")] double start,
      [SchemaParamInfo("Extrusion end")] double end,
      [SchemaParamInfo("Profile along which to extrude the roof"), SchemaMainParam] Line referenceLine,
      Level level,
      List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.parameters = parameters.ToBase();
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
    public double? slope { get; set; }

    public RevitFootprintRoof() { }

    [SchemaInfo("RevitFootprintRoof", "Creates a Revit roof by outline", "Revit", "Architecture")]
    public RevitFootprintRoof([SchemaMainParam] ICurve outline, string family, string type, Level level, RevitLevel cutOffLevel = null, double slope = 0, List<ICurve> voids = null,
      List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.family = family;
      this.type = type;
      this.slope = slope;
      this.parameters = parameters.ToBase();
      this.level = level;
      this.cutOffLevel = cutOffLevel;
      this.elements = elements;
    }
  }
}
