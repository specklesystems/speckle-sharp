using Objects.Structural.Materials;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Materials;

public class GSAConcrete : Concrete
{
  public GSAConcrete() { }

  [SchemaInfo("GSAConcrete", "Creates a Speckle structural concrete material for GSA", "GSA", "Materials")]
  public GSAConcrete(
    int nativeId,
    string name,
    string grade = null,
    string designCode = null,
    string codeYear = null,
    double elasticModulus = 0,
    double compressiveStrength = 0,
    double tensileStrength = 0,
    double flexuralStrength = 0,
    double maxCompressiveStrain = 0,
    double maxTensileStrain = 0,
    double maxAggregateSize = 0,
    bool lightweight = false,
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
    this.compressiveStrength = compressiveStrength;
    this.tensileStrength = tensileStrength;
    this.flexuralStrength = flexuralStrength;
    this.maxCompressiveStrain = maxCompressiveStrain;
    this.maxTensileStrain = maxTensileStrain;
    this.maxAggregateSize = maxAggregateSize;
    this.lightweight = lightweight;
    this.poissonsRatio = poissonsRatio;
    this.shearModulus = shearModulus;
    this.density = density;
    thermalExpansivity = thermalExpansivity;
    this.dampingRatio = dampingRatio;
    this.cost = cost;
    this.colour = colour;
  }

  public int nativeId { get; set; }
  public string colour { get; set; }

  // FROM GWA
  //public string Name { get => name; set { name = value; } }
  //public GsaMat Mat;
  //public MatConcreteType Type;
  //public MatConcreteCement Cement;
  //public double? Fc; //
  //public double? Fcd; //
  //public double? Fcdc; //
  //public double? Fcdt; //
  //public double? Fcfib;
  //public double? EmEs;
  //public double? N;
  //public double? Emod;
  //public double? EpsPeak;
  //public double? EpsMax;
  //public double? EpsU; // have tens and comp represented separately
  //public double? EpsAx; // have tens and comp represented separately
  //public double? EpsTran;
  //public double? EpsAxs;
  //public bool Light; // add this
  //public double? Agg;
  //public double? XdMin;
  //public double? XdMax;
  //public double? Beta;
  //public double? Shrink;
  //public double? Confine;
  //public double? Fcc;
  //public double? EpsPlasC;
  //public double? EpsUC;
}
