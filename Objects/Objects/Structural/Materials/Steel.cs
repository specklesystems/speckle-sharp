using Speckle.Core.Kits;

namespace Objects.Structural.Materials;

public class Steel : StructuralMaterial
{
  public Steel() { }

  [SchemaInfo(
    "Steel",
    "Creates a Speckle structural material for steel (to be used in structural analysis models)",
    "Structural",
    "Materials"
  )]
  public Steel(
    string name,
    string? grade = null,
    string? designCode = null,
    string? codeYear = null,
    double elasticModulus = 0,
    double yieldStrength = 0,
    double ultimateStrength = 0,
    double maxStrain = 0,
    double poissonsRatio = 0,
    double shearModulus = 0,
    double density = 0,
    double alpha = 0,
    double dampingRatio = 0
  )
  {
    this.name = name;
    this.grade = grade;
    materialType = MaterialType.Steel;
    this.designCode = designCode;
    this.codeYear = codeYear;
    this.elasticModulus = elasticModulus;
    this.yieldStrength = yieldStrength;
    this.ultimateStrength = ultimateStrength;
    this.maxStrain = maxStrain;
    this.poissonsRatio = poissonsRatio;
    this.shearModulus = shearModulus;
    this.density = density;
    this.dampingRatio = dampingRatio;
  }

  public double yieldStrength { get; set; } //or yieldStress
  public double ultimateStrength { get; set; } //ultimateStress
  public double maxStrain { get; set; } //failureStrain
  public double strainHardeningModulus { get; set; }
}
