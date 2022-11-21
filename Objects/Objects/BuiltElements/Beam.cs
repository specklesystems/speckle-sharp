using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Beam : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Beam() { }

    [SchemaInfo("Beam", "Creates a Speckle beam", "BIM", "Structure")]
    public Beam([SchemaMainParam] ICurve baseLine)
    {
      this.baseLine = baseLine;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitBeam : Beam
  {
    public string family { get; set; }
    public string type { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitBeam() { }

    [SchemaInfo("RevitBeam", "Creates a Revit beam by curve and base level.", "Revit", "Structure")]
    public RevitBeam(string family, string type, [SchemaMainParam] ICurve baseLine, Level level, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.parameters = parameters.ToBase();
      this.level = level;
    }
  }
}

namespace Objects.BuiltElements.TeklaStructures
{
  public class TeklaBeam : Beam, IHasVolume, IHasArea
  {
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
    public double volume { get; set; }
    public double area { get; set ; }

    public TeklaBeam() { }

    [SchemaInfo("TeklaBeam", "Creates a Tekla Structures beam by curve.", "Tekla", "Structure")]
    public TeklaBeam([SchemaMainParam] ICurve baseLine, SectionProfile profile, StructuralMaterial material)
    {
      this.baseLine = baseLine;
      this.profile = profile;
      this.material = material;
    }
  }
  public class SpiralBeam : TeklaBeam
  {
    public SpiralBeam()
    {
    }

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
  public class Segment : Base
  {
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool circleBased { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string modelElemStructureType { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double nominalHeight { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double nominalWidth { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool isHomogeneous { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double endWith { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double endHeight { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool isEndWidthAndHeightLinked { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool isWidthAndHeightLinked { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string profileAttrName { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string buildingMaterial { get; set; }
  }

  public class Scheme : Base
  {
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string lengthType { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double fixedLength { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double lengthProportion { get; set; }
  }

  public class Cut : Base
  {
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string cutType { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double customAngle { get; set; }
  }

  public class Hole : Base
  {
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string holeType { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool holeContourOn { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public System.Int32 holeId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double centerx { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double centerz { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double width { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public double height { get; set; }
  }
}

namespace Objects.BuiltElements.Archicad
{
    public class ArchicadBeam : Objects.BuiltElements.Beam
    {
        public int? floorIndex { get; set; }
        public Point begC { get; set; }
        public Point endC { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? aboveViewLinePen { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? refPen { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? cutContourLinePen { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public System.Int32? sequence { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? isAutoOnStoryVisibility { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? offset { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? level { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? curveAngle { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? verticalCurveHeight { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? beamShape { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? hiddenLinePen { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? anchorPoint { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? belowViewLinePen { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? isFlipped { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? isSlanted { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? slantAngle { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? profileAngle { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public System.Int32? nSegments { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public System.Int32? nCuts { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public System.Int32? nSchemes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public System.Int32? nProfiles { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? useCoverFill { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? useCoverFillFromSurface { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? coverFillOrientationComesFrom3D { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? coverFillForegroundPen { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public short? coverFillBackgroundPen { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? modelElemStructureType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Segment>? Segments { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Scheme>? Schemes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Cut>? Cuts { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Hole>? Holes { get; set; }

        public ArchicadBeam() { }

        [SchemaInfo("ArchicadBeam", "Creates an Archicad Structures beam by curve.", "Archicad", "Structure")]

        public ArchicadBeam (Point begC, Point endC)
        {
            this.begC = begC;
            this.endC = endC;
        }

    }
}