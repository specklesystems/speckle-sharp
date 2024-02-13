using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitFloor : Floor
{
  public RevitFloor() { }

  [SchemaInfo("RevitFloor", "Creates a Revit floor by outline and level", "Revit", "Architecture")]
  public RevitFloor(
    string family,
    string type,
    [SchemaMainParam] ICurve outline,
    Level level,
    bool structural = false,
    double slope = 0,
    Line? slopeDirection = null,
    List<ICurve>? voids = null,
    [SchemaParamInfo("Any nested elements that this floor might have")] List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
  {
    this.family = family;
    this.type = type;
    this.level = level;
    this.structural = structural;
    this.slope = slope;
    this.slopeDirection = slopeDirection;
    this.parameters = parameters?.ToBase();
    this.outline = outline;
    this.voids = voids ?? new();
    this.elements = elements;
  }

  public string family { get; set; }
  public string type { get; set; }

  public new Level? level
  {
    get => base.level;
    set => base.level = value;
  }

  public bool structural { get; set; }
  public double slope { get; set; }
  public Line? slopeDirection { get; set; }
  public Base? parameters { get; set; }
  public string elementId { get; set; }
}
