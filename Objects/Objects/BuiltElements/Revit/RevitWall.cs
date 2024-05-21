using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitWall : Wall
{
  public RevitWall() { }

  /// <summary>
  /// SchemaBuilder constructor for a Revit wall
  /// </summary>
  /// <param name="family"></param>
  /// <param name="type"></param>
  /// <param name="baseLine"></param>
  /// <param name="level"></param>
  /// <param name="topLevel"></param>
  /// <param name="baseOffset"></param>
  /// <param name="topOffset"></param>
  /// <param name="flipped"></param>
  /// <param name="structural"></param>
  /// <param name="elements"></param>
  /// <param name="parameters"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="baseOffset"/> and <paramref name="topOffset"/> params</remarks>
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
  {
    this.family = family;
    this.type = type;
    this.baseLine = baseLine;
    this.baseOffset = baseOffset;
    this.topOffset = topOffset;
    this.flipped = flipped;
    this.structural = structural;
    this.level = level;
    this.topLevel = topLevel;
    this.elements = elements;
    this.parameters = parameters?.ToBase();
  }

  /// <summary>
  /// SchemaBuilder constructor for a Revit wall
  /// </summary>
  /// <param name="family"></param>
  /// <param name="type"></param>
  /// <param name="baseLine"></param>
  /// <param name="level"></param>
  /// <param name="height"></param>
  /// <param name="baseOffset"></param>
  /// <param name="topOffset"></param>
  /// <param name="flipped"></param>
  /// <param name="structural"></param>
  /// <param name="elements"></param>
  /// <param name="parameters"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="height"/>, <paramref name="baseOffset"/>, and <paramref name="topOffset"/> params</remarks>
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
  {
    this.family = family;
    this.type = type;
    this.baseLine = baseLine;
    this.height = height;
    this.baseOffset = baseOffset;
    this.topOffset = topOffset;
    this.flipped = flipped;
    this.structural = structural;
    this.level = level;
    this.elements = elements;
    this.parameters = parameters?.ToBase();
  }

  public string family { get; set; }
  public string type { get; set; }
  public double baseOffset { get; set; }
  public double topOffset { get; set; }
  public bool flipped { get; set; }
  public bool structural { get; set; }

  public new Level? level
  {
    get => base.level;
    set => base.level = value;
  }

  public Level topLevel { get; set; }
  public Base? parameters { get; set; }
  public string elementId { get; set; }
}
