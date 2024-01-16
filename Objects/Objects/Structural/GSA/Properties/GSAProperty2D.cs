using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Properties;

public class GSAProperty2D : Property2D
{
  public GSAProperty2D() { }

  [SchemaInfo("GSAProperty2D", "Creates a Speckle structural 2D element property for GSA", "GSA", "Properties")]
  public GSAProperty2D(int nativeId, string name, StructuralMaterial material, double thickness)
  {
    this.nativeId = nativeId;
    this.name = name;
    this.material = material;
    this.thickness = thickness;
  }

  public int nativeId { get; set; }

  [DetachProperty]
  public StructuralMaterial designMaterial { get; set; }

  public double cost { get; set; }
  public double additionalMass { get; set; }
  public string concreteSlabProp { get; set; }
  public string colour { get; set; }
}
