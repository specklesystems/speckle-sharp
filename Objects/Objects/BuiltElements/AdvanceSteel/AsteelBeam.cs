using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel;

public class AsteelBeam : Beam, IHasVolume, IHasArea, IAsteelObject
{
  [DetachProperty]
  public SectionProfile profile { get; set; }

  [DetachProperty]
  public StructuralMaterial material { get; set; }

  [DetachProperty]
  public AsteelSectionProfile asteelProfile { get; set; }

  public double volume { get; set; }
  public double area { get; set; }
  public Base userAttributes { get; set; }

  public Base asteelProperties { get; set; }

  public AsteelBeam() { }

  [SchemaInfo("AsteelBeam", "Creates a Advance Steel beam by curve.", "Advance Steel", "Structure")]
  public AsteelBeam([SchemaMainParam] ICurve baseLine, SectionProfile profile, StructuralMaterial material)
  {
    this.baseLine = baseLine;
    this.profile = profile;
    this.material = material;
  }
}
