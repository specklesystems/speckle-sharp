using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Beam : Base, IDisplayValue<List<Mesh>>
  {
    public Beam() { }

    [SchemaInfo("Beam", "Creates a Speckle beam", "BIM", "Structure")]
    public Beam([SchemaMainParam] ICurve baseLine)
    {
      this.baseLine = baseLine;
    }

    public ICurve baseLine { get; set; }

    public virtual Level? level { get; internal set; }

    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitBeam : Beam
  {
    public RevitBeam() { }

    [SchemaInfo("RevitBeam", "Creates a Revit beam by curve and base level.", "Revit", "Structure")]
    public RevitBeam(
      string family,
      string type,
      [SchemaMainParam] ICurve baseLine,
      Level level,
      List<Parameter> parameters = null
    )
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.parameters = parameters.ToBase();
      this.level = level;
    }

    public string family { get; set; }
    public string type { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public new Level? level
    {
      get => base.level;
      set => base.level = value;
    }
  }
}

namespace Objects.BuiltElements.TeklaStructures
{
  public class TeklaBeam : Beam, IHasVolume, IHasArea
  {
    public TeklaBeam() { }

    [SchemaInfo("TeklaBeam", "Creates a Tekla Structures beam by curve.", "Tekla", "Structure")]
    public TeklaBeam([SchemaMainParam] ICurve baseLine, SectionProfile profile, StructuralMaterial material)
    {
      this.baseLine = baseLine;
      this.profile = profile;
      this.material = material;
    }

    public string name { get; set; }

    [DetachProperty]
    public SectionProfile profile { get; set; }

    [DetachProperty]
    public StructuralMaterial material { get; set; }

    [DetachProperty]
    public string finish { get; set; }

    [DetachProperty]
    public string classNumber { get; set; }

    public Vector alignmentVector { get; set; } // This can be set to get proper rotation if coming from an application that doesn't have positioning

    [DetachProperty]
    public TeklaPosition position { get; set; }

    public Base userProperties { get; set; }

    [DetachProperty]
    public Base rebars { get; set; }

    public TeklaBeamType TeklaBeamType { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
  }

  public class SpiralBeam : TeklaBeam
  {
    public Point startPoint { get; set; }
    public Point rotationAxisPt1 { get; set; }
    public Point rotationAxisPt2 { get; set; }
    public double totalRise { get; set; }
    public double rotationAngle { get; set; }
    public double twistAngleStart { get; set; }
    public double twistAngleEnd { get; set; }
  }
}

namespace Objects.BuiltElements.Archicad
{
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
}
