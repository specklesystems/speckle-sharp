using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitOpening : Opening
{
  public Base? parameters { get; set; }
  public string elementId { get; set; }
}

public class RevitVerticalOpening : RevitOpening { }

public class RevitWallOpening : RevitOpening
{
  public RevitWallOpening() { }

  [
    Obsolete("Use constructor with Polyline input instead"),
    SchemaDeprecated,
    SchemaInfo("Revit Wall Opening (Deprecated)", "Creates a Speckle Wall opening for revit", "BIM", "Architecture")
  ]
  public RevitWallOpening(ICurve outline, RevitWall? host = null)
  {
    if (outline is not Polyline)
    {
      throw new SpeckleException("Outline should be a rectangular-shaped polyline", false);
    }

    this.outline = outline;
    this.host = host;
  }

  [SchemaInfo("Revit Wall Opening", "Creates a Speckle Wall opening for revit", "Revit", "Architecture")]
  public RevitWallOpening(Polyline outline, RevitWall? host = null)
  {
    if (outline == null)
    {
      throw new SpeckleException("Outline cannot be null");
    }

    if (outline.GetPoints().Count != 4)
    {
      throw new SpeckleException("Outline should be a rectangular-shaped polyline");
    }

    this.outline = outline;
    this.host = host;
  }

  public RevitWall? host { get; set; }
}

public class RevitShaft : RevitOpening
{
  public RevitShaft() { }

  /// <summary>
  /// SchemaBuilder constructor for a Revit shaft
  /// </summary>
  /// <param name="outline"></param>
  /// <param name="bottomLevel"></param>
  /// <param name="topLevel"></param>
  /// <param name="parameters"></param>
  [SchemaInfo("RevitShaft", "Creates a Revit shaft from a bottom and top level", "Revit", "Architecture")]
  public RevitShaft(
    [SchemaMainParam] ICurve outline,
    Level bottomLevel,
    Level topLevel,
    List<Parameter>? parameters = null
  )
  {
    this.outline = outline;
    this.bottomLevel = bottomLevel;
    this.topLevel = topLevel;
    this.parameters = parameters?.ToBase();
  }

  public Level bottomLevel { get; set; }
  public Level topLevel { get; set; }
  public double height { get; set; }

  /*
  /// <summary>
  /// SchemaBuilder constructor for a Revit shaft
  /// </summary>
  /// <param name="outline"></param>
  /// <param name="bottomLevel"></param>
  /// <param name="height"></param>
  /// <param name="parameters"></param>
  /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
  [SchemaInfo("RevitShaft", "Creates a Revit shaft from a bottom level and height")]
  public RevitShaft(ICurve outline, Level bottomLevel, double height, List<Parameter> parameters = null)
  {
    this.outline = outline;
    this.bottomLevel = bottomLevel;
    this.height = height;
    this.parameters = parameters.ToBase();
  }
  */
}
