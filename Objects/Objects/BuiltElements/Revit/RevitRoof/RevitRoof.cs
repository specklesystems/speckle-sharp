using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit.RevitRoof;

public class RevitRoof : Roof
{
  public string family { get; set; }
  public string type { get; set; }
  public Base? parameters { get; set; }
  public string elementId { get; set; }

  public new Level? level
  {
    get => base.level;
    set => base.level = value;
  }
}

public class RevitExtrusionRoof : RevitRoof
{
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
  public RevitExtrusionRoof(
    string family,
    string type,
    [SchemaParamInfo("Extrusion start")] double start,
    [SchemaParamInfo("Extrusion end")] double end,
    [SchemaParamInfo("Profile along which to extrude the roof"), SchemaMainParam] Line referenceLine,
    Level level,
    List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
  {
    this.family = family;
    this.type = type;
    this.parameters = parameters?.ToBase();
    this.level = level;
    this.start = start;
    this.end = end;
    this.referenceLine = referenceLine;
    this.elements = elements;
  }

  public double start { get; set; }
  public double end { get; set; }
  public Line referenceLine { get; set; }
}

public class RevitFootprintRoof : RevitRoof
{
  public RevitFootprintRoof() { }

  [SchemaInfo("RevitFootprintRoof", "Creates a Revit roof by outline", "Revit", "Architecture")]
  public RevitFootprintRoof(
    [SchemaMainParam] ICurve outline,
    string family,
    string type,
    Level level,
    RevitLevel? cutOffLevel = null,
    double slope = 0,
    List<ICurve>? voids = null,
    List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
  {
    this.outline = outline;
    this.voids = voids ?? new();
    this.family = family;
    this.type = type;
    this.slope = slope;
    this.parameters = parameters?.ToBase();
    this.level = level;
    this.cutOffLevel = cutOffLevel;
    this.elements = elements;
  }

  public RevitLevel? cutOffLevel { get; set; }
  public double? slope { get; set; }
}
