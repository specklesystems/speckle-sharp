using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitWall : Wall
{
  public RevitWall() { }

  public RevitWall(
    string family,
    string type,
    ICurve baseLine,
    Level level,
    Level? topLevel,
    double height,
    string? units,
    string? elementId,
    double baseOffset = 0,
    double topOffset = 0,
    bool flipped = false,
    bool structural = false,
    IReadOnlyList<Mesh>? displayValue = null,
    List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
    : base(height, units, baseLine, level, displayValue, elements)
  {
    this.family = family;
    this.type = type;
    this.baseOffset = baseOffset;
    this.topOffset = topOffset;
    this.flipped = flipped;
    this.structural = structural;
    this.elementId = elementId;
    this.topLevel = topLevel;
    this.parameters = parameters?.ToBase();
  }

  public string family { get; set; }
  public string type { get; set; }
  public double baseOffset { get; set; }
  public double topOffset { get; set; }
  public bool flipped { get; set; }
  public bool structural { get; set; }
  public Level? topLevel { get; set; }
  public Base? parameters { get; set; }
  public string? elementId { get; set; }

  #region Schema Info Constructors

  [SchemaInfo(
    "RevitWall by curve and levels",
    "Creates a Revit wall with a top and base level.",
    "Revit",
    "Architecture"
  )]
  public RevitWall(
    string family,
    string type,
    [SchemaMainParam] ICurve baseLine,
    Level level,
    Level topLevel,
    double baseOffset = 0,
    double topOffset = 0,
    bool flipped = false,
    bool structural = false,
    [SchemaParamInfo("Set in here any nested elements that this level might have.")] List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
    : this(
      family,
      type,
      baseLine,
      level,
      topLevel,
      0,
      null,
      null,
      baseOffset,
      topOffset,
      flipped,
      structural,
      elements: elements,
      parameters: parameters
    ) { }

  [SchemaInfo("RevitWall by curve and height", "Creates an unconnected Revit wall.", "Revit", "Architecture")]
  public RevitWall(
    string family,
    string type,
    [SchemaMainParam] ICurve baseLine,
    Level level,
    double height,
    double baseOffset = 0,
    double topOffset = 0,
    bool flipped = false,
    bool structural = false,
    [SchemaParamInfo("Set in here any nested elements that this wall might have.")] List<Base>? elements = null,
    List<Parameter>? parameters = null
  )
    : this(
      family,
      type,
      baseLine,
      level,
      null,
      height,
      null,
      null,
      baseOffset,
      topOffset,
      flipped,
      structural,
      elements: elements,
      parameters: parameters
    ) { }
  #endregion
}
