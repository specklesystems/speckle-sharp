using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Column : Base, IDisplayValue<List<Mesh>>
  {
    public Column() { }

    [SchemaInfo("Column", "Creates a Speckle column", "BIM", "Structure")]
    public Column([SchemaMainParam] ICurve baseLine)
    {
      this.baseLine = baseLine;
    }

    public ICurve baseLine { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitColumn : Column
  {
    public RevitColumn() { }

    /// <summary>
    /// SchemaBuilder constructor for a Revit column
    /// </summary>
    /// <param name="family"></param>
    /// <param name="type"></param>
    /// <param name="baseLine"></param>
    /// <param name="level"></param>
    /// <param name="topLevel"></param>
    /// <param name="baseOffset"></param>
    /// <param name="topOffset"></param>
    /// <param name="structural"></param>
    /// <param name="rotation"></param>
    /// <param name="parameters"></param>
    /// <remarks>Assign units when using this constructor due to <paramref name="baseOffset"/> and <paramref name="topOffset"/> params</remarks>
    [SchemaInfo(
      "RevitColumn Vertical",
      "Creates a vertical Revit Column by point and levels.",
      "Revit",
      "Architecture"
    )]
    public RevitColumn(
      string family,
      string type,
      [SchemaParamInfo("Only the lower point of this line will be used as base point."), SchemaMainParam]
        ICurve baseLine,
      Level level,
      Level topLevel,
      double baseOffset = 0,
      double topOffset = 0,
      bool structural = false,
      [SchemaParamInfo("Rotation angle in radians")] double rotation = 0,
      List<Parameter> parameters = null
    )
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.topLevel = topLevel;
      this.baseOffset = baseOffset;
      this.topOffset = topOffset;
      //this.structural = structural;
      this.rotation = rotation;
      this.parameters = parameters.ToBase();
      this.level = level;
    }

    [
      SchemaDeprecated,
      SchemaInfo("RevitColumn Slanted (old)", "Creates a slanted Revit Column by curve.", "Revit", "Structure")
    ]
    public RevitColumn(
      string family,
      string type,
      [SchemaMainParam] ICurve baseLine,
      Level level,
      bool structural = false,
      List<Parameter> parameters = null
    )
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.level = level;
      //this.structural = structural;
      isSlanted = true;
      this.parameters = parameters.ToBase();
    }

    [SchemaInfo("RevitColumn Slanted", "Creates a slanted Revit Column by curve.", "Revit", "Structure")]
    public RevitColumn(
      string family,
      string type,
      [SchemaMainParam] ICurve baseLine,
      Level level,
      Level topLevel = null,
      bool structural = false,
      List<Parameter> parameters = null
    )
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.level = level;
      this.topLevel = topLevel;
      //this.structural = structural;
      isSlanted = true;
      this.parameters = parameters.ToBase();
    }

    public Level level { get; set; }
    public Level topLevel { get; set; }
    public double baseOffset { get; set; }
    public double topOffset { get; set; }
    public bool facingFlipped { get; set; }

    public bool handFlipped { get; set; }

    //public bool structural { get; set; }
    public double rotation { get; set; }
    public bool isSlanted { get; set; }
    public string family { get; set; }
    public string type { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
  }
}

namespace Objects.BuiltElements.Archicad
{
  /*
  For further informations about given the variables, visit:
  https://archicadapi.graphisoft.com/documentation/api_columntype
  */
  public class ArchicadColumn : Column
  {
    [SchemaInfo("ArchicadColumn", "Creates an Archicad Column by curve.", "Archicad", "Structure")]
    public ArchicadColumn() { }

    // Element base
    public string? /*APINullabe*/ elementType { get; set; }
    public List<Classification>? /*APINullabe*/ classifications { get; set; }

    public ArchicadLevel? /*APINullabe*/ level { get; set; }

    // Wall geometry
    public Point origoPos { get; set; }
    public double height { get; set; }

    // Positioning - story relation
    public double? /*APINullabe*/ bottomOffset { get; set; }
    public double? /*APINullabe*/ topOffset { get; set; }
    public short? /*APINullabe*/ relativeTopStory { get; set; }

    // Positioning - slanted column
    public bool? /*APINullabe*/ isSlanted { get; set; }
    public double? /*APINullabe*/ slantAngle { get; set; }
    public double? /*APINullabe*/ slantDirectionAngle { get; set; }
    public bool? /*APINullabe*/ isFlipped { get; set; }

    // Positioning - wrapping
    public bool? /*APINullabe*/ wrapping { get; set; }

    // Positioning - Defines the relation of column to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
    public string? /*APINullabe*/ columnRelationToZoneName { get; set; }

    // End Cuts
    public uint? /*APINullabe*/ nCuts { get; set; }
    public Dictionary<string, AssemblySegmentCut>? /*APINullabe*/ Cuts { get; set; }

    // Reference Axis
    public short? coreAnchor { get; set; }
    public double? axisRotationAngle { get; set; }

    // Segment
    public uint? /*APINullabe*/ nSegments { get; set; }
    public uint? /*APINullabe*/ nProfiles { get; set; }
    public Dictionary<string, ColumnSegment>? /*APINullabe*/ segments { get; set; }

    // Scheme
    public uint? nSchemes { get; set; }
    public Dictionary<string, AssemblySegmentScheme>? Schemes { get; set; }

    // Floor Plan and Section - Floor Plan Display
    public string? /*APINullabe*/ showOnStories { get; set; }
    public string? /*APINullabe*/ displayOptionName { get; set; }
    public string? /*APINullabe*/ showProjectionName { get; set; }

    // Floor Plan and Section - Cut Surfaces
    public short? corePen { get; set; }
    public string? contLtype { get; set; }
    public short? venLinePen { get; set; }
    public string? venLineType { get; set; }
    public short? overrideCutFillPen { get; set; }
    public short? overrideCutFillBackgroundPen { get; set; }

    // Floor Plan and Section - Outlines
    public short? /*APINullabe*/ uncutLinePen { get; set; }
    public string? /*APINullabe*/ uncutLinetype { get; set; }
    public short? /*APINullabe*/ overheadLinePen { get; set; }
    public string? /*APINullabe*/ overheadLinetype { get; set; }
    public short? /*APINullabe*/ hiddenLinePen { get; set; }
    public string? /*APINullabe*/ hiddenLinetype { get; set; }

    // Floor Plan and Section - Floor Plan Symbol
    public string? /*APINullabe*/ coreSymbolTypeName { get; set; }
    public double? /*APINullabe*/ coreSymbolPar1 { get; set; }
    public double? /*APINullabe*/ coreSymbolPar2 { get; set; }
    public short? /*APINullabe*/ coreSymbolPen { get; set; }

    // Floor Plan and Section - Cover Fills
    public bool? /*APINullabe*/ useCoverFill { get; set; }
    public bool? useCoverFillFromSurface { get; set; }
    public short? coverFillForegroundPen { get; set; }
    public short? coverFillBackgroundPen { get; set; }
    public string? coverFillType { get; set; }
    public string? coverFillTransformationType { get; set; }
    public double? coverFillTransformationOrigoX { get; set; }
    public double? coverFillTransformationOrigoY { get; set; }
    public double? coverFillTransformationXAxisX { get; set; }
    public double? coverFillTransformationXAxisY { get; set; }
    public double? coverFillTransformationYAxisX { get; set; }
    public double? coverFillTransformationYAxisY { get; set; }

    public class ColumnSegment : Base
    {
      // Segment - Veneer attributes
      public string? veneerType { get; set; }
      public string? veneerBuildingMaterial { get; set; }

      public double? veneerThick { get; set; }

      // Segment - The extrusion overridden material name
      public string? extrusionSurfaceMaterial { get; set; }

      // Segment - The ends overridden material name
      public string? endsSurfaceMaterial { get; set; }

      // Segment - The overridden materials are chained
      public bool? materialChained { get; set; }
      public AssemblySegment assemblySegmentData { get; set; }
    }
  }
}
