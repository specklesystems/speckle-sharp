using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad;

/*
For further informations about given the variables, visit:
https://archicadapi.graphisoft.com/documentation/api_columntype
*/
public class ArchicadColumn : Column
{
  [SchemaInfo("ArchicadColumn", "Creates an Archicad Column by curve.", "Archicad", "Structure")]
  public ArchicadColumn() { }

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

  // Wall geometry
  public Point origoPos { get; set; }
  public double height { get; set; }

  // Positioning - story relation
  public double? bottomOffset { get; set; } /*APINullabe*/
  public double? topOffset { get; set; } /*APINullabe*/
  public short? relativeTopStory { get; set; } /*APINullabe*/

  // Positioning - slanted column
  public bool? isSlanted { get; set; } /*APINullabe*/
  public double? slantAngle { get; set; } /*APINullabe*/
  public double? slantDirectionAngle { get; set; } /*APINullabe*/
  public bool? isFlipped { get; set; } /*APINullabe*/

  // Positioning - wrapping
  public bool? wrapping { get; set; } /*APINullabe*/

  // Positioning - Defines the relation of column to zones (Zone Boundary, Reduce Zone Area Only, No Effect on Zones)
  public string? columnRelationToZoneName { get; set; } /*APINullabe*/

  // End Cuts
  public uint? nCuts { get; set; } /*APINullabe*/
  public Dictionary<string, AssemblySegmentCut>? Cuts { get; set; } /*APINullabe*/

  // Reference Axis
  public short? coreAnchor { get; set; }
  public double? axisRotationAngle { get; set; }

  // Segment
  public uint? nSegments { get; set; } /*APINullabe*/
  public uint? nProfiles { get; set; } /*APINullabe*/
  public Dictionary<string, ColumnSegment>? segments { get; set; } /*APINullabe*/

  // Scheme
  public uint? nSchemes { get; set; }
  public Dictionary<string, AssemblySegmentScheme>? Schemes { get; set; }

  // Floor Plan and Section - Floor Plan Display
  public string? showOnStories { get; set; } /*APINullabe*/
  public string? displayOptionName { get; set; } /*APINullabe*/
  public string? showProjectionName { get; set; } /*APINullabe*/

  // Floor Plan and Section - Cut Surfaces
  public short? corePen { get; set; }
  public string? contLtype { get; set; }
  public short? venLinePen { get; set; }
  public string? venLineType { get; set; }
  public short? overrideCutFillPen { get; set; }
  public short? overrideCutFillBackgroundPen { get; set; }

  // Floor Plan and Section - Outlines
  public short? uncutLinePen { get; set; } /*APINullabe*/
  public string? uncutLinetype { get; set; } /*APINullabe*/
  public short? overheadLinePen { get; set; } /*APINullabe*/
  public string? overheadLinetype { get; set; } /*APINullabe*/
  public short? hiddenLinePen { get; set; } /*APINullabe*/
  public string? hiddenLinetype { get; set; } /*APINullabe*/

  // Floor Plan and Section - Floor Plan Symbol
  public string? coreSymbolTypeName { get; set; } /*APINullabe*/
  public double? coreSymbolPar1 { get; set; } /*APINullabe*/
  public double? coreSymbolPar2 { get; set; } /*APINullabe*/
  public short? coreSymbolPen { get; set; } /*APINullabe*/

  // Floor Plan and Section - Cover Fills
  public bool? useCoverFill { get; set; } /*APINullabe*/
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
