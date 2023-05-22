using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Other;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Roof : Base, IDisplayValue<List<Mesh>>
  {
    public Roof() { }

    [SchemaDeprecated, SchemaInfo("Roof", "Creates a Speckle roof", "BIM", "Architecture")]
    public Roof([SchemaMainParam] ICurve outline, List<ICurve> voids = null, List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }

    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new();

    [DetachProperty]
    public List<Base> elements { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
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
      List<Base> elements = null,
      List<Parameter> parameters = null
    )
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
      RevitLevel cutOffLevel = null,
      double slope = 0,
      List<ICurve> voids = null,
      List<Base> elements = null,
      List<Parameter> parameters = null
    )
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

    public RevitLevel cutOffLevel { get; set; }
    public double? slope { get; set; }
  }
}


namespace Objects.BuiltElements.Archicad
{
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

    // Geometry and positioning
    public Level level { get; set; }
    public double? thickness { get; set; }
    public string structure { get; set; }
    public string? compositeName { get; set; }
    public string? buildingMaterialName { get; set; }

    // EdgeTrims
    public string? edgeAngleType { get; set; }
    public double? edgeAngle { get; set; }

    // Floor Plan and Section - Floor Plan Display
    public string showOnStories { get; set; }
    public Visibility? visibilityCont { get; set; }
    public Visibility? visibilityFill { get; set; }
    public string displayOptionName { get; set; }
    public string showProjectionName { get; set; }

    // Floor Plan and Section - Cut Surfaces
    public short sectContPen { get; set; }
    public string sectContLtype { get; set; }
    public short? cutFillPen { get; set; }
    public short? cutFillBackgroundPen { get; set; }

    // Floor Plan and Section - Outlines
    public short contourPen { get; set; }
    public string contourLineType { get; set; }
    public short overheadLinePen { get; set; }
    public string overheadLinetype { get; set; }

    // Floor Plan and Section - Cover Fills
    public bool useFloorFill { get; set; }
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
    public string trimmingBodyName { get; set; }
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
      public Int32? nLevelEdgeData { get; set; }
      public Dictionary<string, LevelEdge>? roofLevels { get; set; }
    }

    // Geometry and positioning
    public string roofClassName { get; set; }
    public double? planeRoofAngle { get; set; }
    public ElementShape shape { get; set; }
    public BaseLine? baseLine { get; set; }
    public bool? posSign { get; set; }
    public ElementShape pivotPolygon { get; set; }
    public short? levelNum { get; set; }
    public Dictionary<string, RoofLevel> levels { get; set; }
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
    public string shellClassName { get; set; }
    public Transform basePlane { get; set; }
    public bool flipped { get; set; }
    public bool hasContour { get; set; }
    public int numHoles { get; set; }
    public Dictionary<string, ShellContourData>? shellContours { get; set; }
    public string defaultEdgeType { get; set; }

    public double? slantAngle { get; set; }
    public double? revolutionAngle { get; set; }
    public double? distortionAngle { get; set; }
    public bool? segmentedSurfaces { get; set; }
    public double? shapePlaneTilt { get; set; }
    public double? begPlaneTilt { get; set; }
    public double? endPlaneTilt { get; set; }
    public ElementShape shape { get; set; }
    public ElementShape shape1 { get; set; }
    public ElementShape shape2 { get; set; }
    public Transform axisBase { get; set; }
    public Transform plane1 { get; set; }
    public Transform plane2 { get; set; }
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
}
