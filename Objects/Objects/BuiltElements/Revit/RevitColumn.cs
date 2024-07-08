using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitColumn : Column
{
  public RevitColumn() { }

  public RevitColumn(
    string family,
    string type,
    ICurve baseLine,
    Level? level,
    Level? topLevel,
    string? units,
    string? elementId,
    double baseOffset = 0,
    double topOffset = 0,
    bool facingFlipped = false,
    bool handFlipped = false,
    bool isSlanted = false,
    double rotation = 0,
    IReadOnlyList<Mesh>? displayValue = null,
    List<Parameter>? parameters = null
  )
    : base(baseLine, units, level, displayValue)
  {
    this.family = family;
    this.type = type;
    this.topLevel = topLevel;
    this.elementId = elementId;
    this.baseOffset = baseOffset;
    this.topOffset = topOffset;
    this.facingFlipped = facingFlipped;
    this.handFlipped = handFlipped;
    this.isSlanted = isSlanted;
    this.rotation = rotation;
    this.parameters = parameters?.ToBase();
  }

  public Level? topLevel { get; set; }
  public double baseOffset { get; set; }
  public double topOffset { get; set; }
  public bool facingFlipped { get; set; }
  public bool handFlipped { get; set; }
  public double rotation { get; set; }
  public bool isSlanted { get; set; }
  public string family { get; set; }
  public string type { get; set; }
  public Base? parameters { get; set; }
  public string? elementId { get; set; }

  #region Schema Info Constructors

  [SchemaInfo("RevitColumn Vertical", "Creates a vertical Revit Column by point and levels.", "Revit", "Architecture")]
  public RevitColumn(
    string family,
    string type,
    [SchemaParamInfo("Only the lower point of this line will be used as base point."), SchemaMainParam] ICurve baseLine,
    Level level,
    Level topLevel,
    double baseOffset = 0,
    double topOffset = 0,
    bool structural = false,
    [SchemaParamInfo("Rotation angle in radians")] double rotation = 0,
    List<Parameter>? parameters = null
  )
    : this(
      family,
      type,
      baseLine,
      level,
      topLevel,
      null,
      null,
      baseOffset,
      topOffset,
      rotation: rotation,
      parameters: parameters
    ) { }

  [Obsolete("Use other constructors")]
  [SchemaDeprecated]
  [SchemaInfo("RevitColumn Slanted (old)", "Creates a slanted Revit Column by curve.", "Revit", "Structure")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Style",
    "IDE0060:Remove unused parameter",
    Justification = "Obsolete"
  )]
  public RevitColumn(
    string family,
    string type,
    [SchemaMainParam] ICurve baseLine,
    Level level,
    bool structural = false,
    List<Parameter>? parameters = null
  )
  {
    this.family = family;
    this.type = type;
    this.baseLine = baseLine;
    this.level = level;
    isSlanted = true;
    this.parameters = parameters?.ToBase();
  }

  [SchemaInfo("RevitColumn Slanted", "Creates a slanted Revit Column by curve.", "Revit", "Structure")]
  public RevitColumn(
    string family,
    string type,
    [SchemaMainParam] ICurve baseLine,
    Level level,
    Level? topLevel = null,
    bool structural = false,
    List<Parameter>? parameters = null
  )
    : this(family, type, baseLine, level, topLevel, null, null, displayValue: null, parameters: parameters) { }

  #endregion
}
