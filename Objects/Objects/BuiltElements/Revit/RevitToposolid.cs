using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitToposolid : Base, IDisplayValue<List<Mesh>>
{
  public RevitToposolid() { }

  [SchemaInfo("RevitToposolid", "Creates a Revit Toposolid", "Revit", "Architecture")]
  public RevitToposolid(
    Level level,
    List<Polycurve> profiles,
    List<Point>? topPlanePoints = null,
    [SchemaParamInfo("Any nested elements that this floor might have")] List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
  {
    this.profiles = profiles;
    this.level = level;
    this.points = topPlanePoints ?? new();
    this.elements = elements;
    this.parameters = parameters?.ToBase();
  }

  public List<Polycurve> profiles { get; set; } = new();

  public List<Point> points { get; set; } = new();

  [DetachProperty]
  public List<Base>? elements { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public string family { get; set; }
  public string type { get; set; }
  public Level level { get; set; }
  public Base? parameters { get; set; }
}
