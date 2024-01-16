using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.TeklaStructures;

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
