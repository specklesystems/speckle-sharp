using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Archicad;

/*
For further informations about given the variables, visit:
https://archicadapi.graphisoft.com/documentation/api_beamtype
*/
public class ArchicadBeam : Beam
{
  [SchemaInfo("ArchicadBeam", "Creates an Archicad beam by curve.", "Archicad", "Structure")]
  public ArchicadBeam() { }

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

  // Positioning
  public Point begC { get; set; }
  public Point endC { get; set; }
  public bool? isSlanted { get; set; } /*APINullabe*/
  public double? slantAngle { get; set; } /*APINullabe*/
  public string? beamShape { get; set; } /*APINullabe*/
  public int? sequence { get; set; } /*APINullabe*/
  public double? curveAngle { get; set; } /*APINullabe*/
  public double? verticalCurveHeight { get; set; } /*APINullabe*/
  public bool? isFlipped { get; set; } /*APINullabe*/

  // End Cuts
  public uint? nCuts { get; set; } /*APINullabe*/
  public Dictionary<string, AssemblySegmentCut>? Cuts { get; set; }

  // Reference Axis
  public short? anchorPoint { get; set; } /*APINullabe*/
  public double? offset { get; set; }
  public double? profileAngle { get; set; }

  // Segment
  public uint? nSegments { get; set; } /*APINullabe*/
  public uint? nProfiles { get; set; } /*APINullabe*/
  public Dictionary<string, BeamSegment>? segments { get; set; } /*APINullabe*/

  // Scheme
  public uint? nSchemes { get; set; }
  public Dictionary<string, AssemblySegmentScheme>? Schemes { get; set; }

  // Hole
  public Dictionary<string, Hole>? Holes { get; set; }

  // Floor Plan and Section - Floor Plan Display
  public string? showOnStories { get; set; } /*APINullabe*/
  public string? displayOptionName { get; set; } /*APINullabe*/
  public string? uncutProjectionMode { get; set; } /*APINullabe*/
  public string? overheadProjectionMode { get; set; } /*APINullabe*/
  public string? showProjectionName { get; set; } /*APINullabe*/

  // Floor Plan and Section - Cut Surfaces
  public short? cutContourLinePen { get; set; }
  public string? cutContourLineType { get; set; }
  public short? overrideCutFillPen { get; set; }
  public short? overrideCutFillBackgroundPen { get; set; }

  // Floor Plan and Section - Outlines
  public string? showOutline { get; set; } /*APINullabe*/
  public short? uncutLinePen { get; set; } /*APINullabe*/
  public string? uncutLinetype { get; set; } /*APINullabe*/
  public short? overheadLinePen { get; set; } /*APINullabe*/
  public string? overheadLinetype { get; set; } /*APINullabe*/
  public short? hiddenLinePen { get; set; } /*APINullabe*/
  public string? hiddenLinetype { get; set; } /*APINullabe*/

  // Floor Plan and Section - Symbol
  public string? showReferenceAxis { get; set; } /*APINullabe*/
  public short? referencePen { get; set; } /*APINullabe*/
  public string? referenceLinetype { get; set; } /*APINullabe*/

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

  public class BeamSegment : Base
  {
    // Segment override materials
    public string? leftMaterial { get; set; }
    public string? topMaterial { get; set; }
    public string? rightMaterial { get; set; }
    public string? bottomMaterial { get; set; }

    public string? endsMaterial { get; set; }

    // Segment - The overridden materials are chained
    public bool? materialChained { get; set; }
    public AssemblySegment assemblySegmentData { get; set; }
  }
}
