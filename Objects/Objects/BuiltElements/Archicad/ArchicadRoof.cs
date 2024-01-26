using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad;

/*
For further informations about given the variables, visit:
https://archicadapi.graphisoft.com/documentation/api_shellbasetype
*/
public class ArchicadShellBase : BuiltElements.Roof
{
  public class Visibility : Base
  {
    public bool? showOnHome { get; set; }
    public bool? showAllAbove { get; set; }
    public bool? showAllBelow { get; set; }
    public short? showRelAbove { get; set; }
    public short? showRelBelow { get; set; }
  }

  // Element base
  public string? elementType { get; set; } /*APINullabe*/

  public List<Classification>? classifications { get; set; } /*APINullabe*/
  public Base? elementProperties { get; set; }
  public Base? componentProperties { get; set; }

  public override Level? level
  {
    get => archicadLevel;
    internal set
    {
      if (value is ArchicadLevel or null)
      {
        archicadLevel = value as ArchicadLevel;
      }
      else
      {
        throw new ArgumentException($"Expected object of type {nameof(ArchicadLevel)}");
      }
    }
  }

  [JsonIgnore]
  public ArchicadLevel? archicadLevel { get; set; } /*APINullabe*/

  public string? layer { get; set; } /*APINullabe*/

  // Geometry and positioning
  public double? thickness { get; set; }
  public string? structure { get; set; } /*APINullabe*/
  public string? compositeName { get; set; }
  public string? buildingMaterialName { get; set; }

  // EdgeTrims
  public string? edgeAngleType { get; set; }
  public double? edgeAngle { get; set; }

  // Floor Plan and Section - Floor Plan Display
  public string? showOnStories { get; set; } /*APINullabe*/
  public Visibility? visibilityCont { get; set; }
  public Visibility? visibilityFill { get; set; }
  public string? displayOptionName { get; set; } /*APINullabe*/
  public string? showProjectionName { get; set; } /*APINullabe*/

  // Floor Plan and Section - Cut Surfaces
  public short? sectContPen { get; set; } /*APINullabe*/
  public string? sectContLtype { get; set; } /*APINullabe*/
  public short? cutFillPen { get; set; }
  public short? cutFillBackgroundPen { get; set; }

  // Floor Plan and Section - Outlines
  public short? contourPen { get; set; } /*APINullabe*/
  public string? contourLineType { get; set; } /*APINullabe*/
  public short? overheadLinePen { get; set; } /*APINullabe*/
  public string? overheadLinetype { get; set; } /*APINullabe*/

  // Floor Plan and Section - Cover Fills
  public bool? useFloorFill { get; set; } /*APINullabe*/
  public short? floorFillPen { get; set; }
  public short? floorFillBGPen { get; set; }
  public string? floorFillName { get; set; }
  public bool? use3DHatching { get; set; }
  public bool? useFillLocBaseLine { get; set; }
  public bool? useSlantedFill { get; set; }
  public string? hatchOrientation { get; set; }
  public double? hatchOrientationOrigoX { get; set; }
  public double? hatchOrientationOrigoY { get; set; }
  public double? hatchOrientationXAxisX { get; set; }
  public double? hatchOrientationXAxisY { get; set; }
  public double? hatchOrientationYAxisX { get; set; }
  public double? hatchOrientationYAxisY { get; set; }

  // Model
  public string? topMat { get; set; }
  public string? sideMat { get; set; }
  public string? botMat { get; set; }
  public bool? materialsChained { get; set; }
  public string? trimmingBodyName { get; set; } /*APINullabe*/
}

/*
For further informations about given the variables, visit:
https://archicadapi.graphisoft.com/documentation/api_rooftype
*/
public sealed class ArchicadRoof : ArchicadShellBase
{
  public class BaseLine : Base
  {
    public Point begC { get; set; }
    public Point endC { get; set; }
  }

  public class RoofLevel : Base
  {
    public double? levelHeight { get; set; }
    public double? levelAngle { get; set; }
  }

  public class LevelEdge : Base
  {
    public double? edgeLevelAngle { get; set; }
    public double? eavesOverhang { get; set; }
    public string? topMaterial { get; set; }
    public string? bottomMaterial { get; set; }
    public string? coverFillType { get; set; }
    public string? angleType { get; set; }
  }

  public class PivotPolyEdge : Base
  {
    public int? nLevelEdgeData { get; set; }
    public Dictionary<string, LevelEdge>? roofLevels { get; set; }
  }

  // Geometry and positioning
  public string roofClassName { get; set; }
  public double? planeRoofAngle { get; set; }
  public ElementShape shape { get; set; }
  public BaseLine? baseLine { get; set; }
  public bool? posSign { get; set; }
  public ElementShape? pivotPolygon { get; set; } /*APINullabe*/
  public short? levelNum { get; set; }
  public Dictionary<string, RoofLevel>? levels { get; set; } /*APINullabe*/
  public Dictionary<string, PivotPolyEdge>? roofPivotPolyEdges { get; set; }
}

/*
For further informations about given the variables, visit:
https://archicadapi.graphisoft.com/documentation/api_shelltype
*/
public sealed class ArchicadShell : ArchicadShellBase
{
  public class ShellContourEdgeData : Base
  {
    public string? sideTypeName { get; set; }
    public double? sideAngle { get; set; }
    public string? edgeTypeName { get; set; }
    public string? edgeSideMaterial { get; set; }
  }

  public class ShellContourData : Base
  {
    public ElementShape? shellContourPoly { get; set; }
    public Transform shellContourPlane { get; set; }
    public double? shellContourHeight { get; set; }
    public int? shellContourID { get; set; }
    public Dictionary<string, ShellContourEdgeData>? shellContourEdges { get; set; }
  }

  // Geometry and positioning
  public string? shellClassName { get; set; } /*APINullabe*/
  public Transform? basePlane { get; set; } /*APINullabe*/
  public bool? flipped { get; set; } /*APINullabe*/
  public bool? hasContour { get; set; } /*APINullabe*/
  public int? numHoles { get; set; } /*APINullabe*/
  public Dictionary<string, ShellContourData>? shellContours { get; set; }
  public string? defaultEdgeType { get; set; } /*APINullabe*/

  public double? slantAngle { get; set; }
  public double? revolutionAngle { get; set; }
  public double? distortionAngle { get; set; }
  public bool? segmentedSurfaces { get; set; }
  public double? shapePlaneTilt { get; set; }
  public double? begPlaneTilt { get; set; }
  public double? endPlaneTilt { get; set; }
  public ElementShape shape { get; set; }
  public ElementShape? shape1 { get; set; } /*APINullabe*/
  public ElementShape? shape2 { get; set; } /*APINullabe*/
  public Transform? axisBase { get; set; } /*APINullabe*/
  public Transform? plane1 { get; set; } /*APINullabe*/
  public Transform? plane2 { get; set; } /*APINullabe*/
  public Point? begC { get; set; }
  public double? begAngle { get; set; }
  public Vector? extrusionVector { get; set; }
  public Vector? shapeDirection { get; set; }
  public Vector? distortionVector { get; set; }
  public string? morphingRuleName { get; set; }

  // Model
  public class BegShapeEdge : Base
  {
    public string? begShapeEdgeTrimSideType { get; set; }
    public double? begShapeEdgeTrimSideAngle { get; set; }
    public string? begShapeEdgeSideMaterial { get; set; }
    public string? begShapeEdgeType { get; set; }
  }

  public class EndShapeEdge : Base
  {
    public string? endShapeEdgeTrimSideType { get; set; }
    public double? endShapeEdgeTrimSideAngle { get; set; }
    public string? endShapeEdgeSideMaterial { get; set; }
    public string? endShapeEdgeType { get; set; }
  }

  public class ExtrudedEdge1 : Base
  {
    public string? extrudedEdgeTrimSideType1 { get; set; }
    public double? extrudedEdgeTrimSideAngle1 { get; set; }
    public string? extrudedEdgeSideMaterial1 { get; set; }
    public string? extrudedEdgeType1 { get; set; }
  }

  public class ExtrudedEdge2 : Base
  {
    public string? extrudedEdgeTrimSideType2 { get; set; }
    public double? extrudedEdgeTrimSideAngle2 { get; set; }
    public string? extrudedEdgeSideMaterial2 { get; set; }
    public string? extrudedEdgeType2 { get; set; }
  }

  public class RevolvedEdge1 : Base
  {
    public string? revolvedEdgeTrimSideType1 { get; set; }
    public double? revolvedEdgeTrimSideAngle1 { get; set; }
    public string? revolvedEdgeSideMaterial1 { get; set; }
    public string? revolvedEdgeType1 { get; set; }
  }

  public class RevolvedEdge2 : Base
  {
    public string? revolvedEdgeTrimSideType2 { get; set; }
    public double? revolvedEdgeTrimSideAngle2 { get; set; }
    public string? revolvedEdgeSideMaterial2 { get; set; }
    public string? revolvedEdgeType2 { get; set; }
  }

  public class RuledEdge1 : Base
  {
    public string? ruledEdgeTrimSideType1 { get; set; }
    public double? ruledEdgeTrimSideAngle1 { get; set; }
    public string? ruledEdgeSideMaterial1 { get; set; }
    public string? ruledEdgeType1 { get; set; }
  }

  public class RuledEdge2 : Base
  {
    public string? ruledEdgeTrimSideType2 { get; set; }
    public double? ruledEdgeTrimSideAngle2 { get; set; }
    public string? ruledEdgeSideMaterial2 { get; set; }
    public string? ruledEdgeType2 { get; set; }
  }

  public BegShapeEdge? begShapeEdge { get; set; }
  public EndShapeEdge? endShapeEdge { get; set; }
  public ExtrudedEdge1? extrudedEdge1 { get; set; }
  public ExtrudedEdge2? extrudedEdge2 { get; set; }
  public RevolvedEdge1? revolvedEdge1 { get; set; }
  public RevolvedEdge2? revolvedEdge2 { get; set; }
  public RuledEdge1? ruledEdge1 { get; set; }
  public RuledEdge2? ruledEdge2 { get; set; }
}
