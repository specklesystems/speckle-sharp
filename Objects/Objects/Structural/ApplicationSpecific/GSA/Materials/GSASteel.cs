using Objects.Structural.Materials;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Materials;

public class GSASteel : Steel
{
  public GSASteel() { }

  [SchemaInfo(
    "Steel",
    "Creates a Speckle structural material for steel (to be used in structural analysis models)",
    "Structural",
    "Materials"
  )]
  public GSASteel(
    int nativeId,
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
    double dampingRatio = 0,
    double cost = 0,
    string colour = "NO_RGB"
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.grade = grade;
    materialType = MaterialType.Concrete;
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
    this.cost = cost;
    this.colour = colour;
  }

  public int nativeId { get; set; }
  public string colour { get; set; }

  // FROM GWA
  //public string Name { get => name; set { name = value; } }
  //public GsaMat Mat;
  //public double? Fy;
  //public double? Fu;
  //public double? EpsP;
  //public double? Eh; //
}
