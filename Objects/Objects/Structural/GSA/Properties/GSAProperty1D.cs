using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Properties;

public class GSAProperty1D : Property1D
{
  public GSAProperty1D() { }

  [SchemaInfo("GSAProperty1D", "Creates a Speckle structural 1D element property for GSA", "GSA", "Properties")]
  public GSAProperty1D(
    int nativeId,
    string name,
    StructuralMaterial material,
    SectionProfile profile,
    double cost = 0,
    double additionalMass = 0
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.material = material;
    this.profile = profile;
    this.cost = cost;
    this.additionalMass = additionalMass;
  }

  public int nativeId { get; set; }

  [DetachProperty]
  public StructuralMaterial designMaterial { get; set; }

  public double additionalMass { get; set; }
  public double? cost { get; set; }
  public int? poolRef { get; set; }
  public string colour { get; set; }
}
