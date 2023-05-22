using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

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
    public Level level { get; set; }
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

    // Positioning
    public ArchicadLevel level { get; set; }
    public Point begC { get; set; }
    public Point endC { get; set; }
    public bool isSlanted { get; set; }
    public double slantAngle { get; set; }
    public string beamShape { get; set; }
    public int sequence { get; set; }
    public double curveAngle { get; set; }
    public double verticalCurveHeight { get; set; }
    public bool isFlipped { get; set; }

    // End Cuts
    public uint nCuts { get; set; }
    public Dictionary<string, AssemblySegmentCut>? Cuts { get; set; }

    // Reference Axis
    public short anchorPoint { get; set; }
    public double? offset { get; set; }
    public double? profileAngle { get; set; }

    // Segment
    public uint nSegments { get; set; }
    public uint nProfiles { get; set; }
    public Dictionary<string, BeamSegment> segments { get; set; }

    // Scheme
    public uint? nSchemes { get; set; }
    public Dictionary<string, AssemblySegmentScheme>? Schemes { get; set; }

    // Hole
    public Dictionary<string, Hole>? Holes { get; set; }

    // Floor Plan and Section - Floor Plan Display
    public string showOnStories { get; set; }
    public string displayOptionName { get; set; }
    public string uncutProjectionMode { get; set; }
    public string overheadProjectionMode { get; set; }
    public string showProjectionName { get; set; }

    // Floor Plan and Section - Cut Surfaces
    public short? cutContourLinePen { get; set; }
    public string? cutContourLineType { get; set; }
    public short? overrideCutFillPen { get; set; }
    public short? overrideCutFillBackgroundPen { get; set; }

    // Floor Plan and Section - Outlines
    public string showOutline { get; set; }
    public short uncutLinePen { get; set; }
    public string uncutLinetype { get; set; }
    public short overheadLinePen { get; set; }
    public string overheadLinetype { get; set; }
    public short hiddenLinePen { get; set; }
    public string hiddenLinetype { get; set; }

    // Floor Plan and Section - Symbol
    public string showReferenceAxis { get; set; }
    public short referencePen { get; set; }
    public string referenceLinetype { get; set; }

    // Floor Plan and Section - Cover Fills
    public bool useCoverFill { get; set; }
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
