using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;

namespace Objects.Structural.GSA.Materials
{
  public class GSAMaterial : Material
  {
    public int nativeId { get; set; }
    public string colour { get; set; }

    public GSAMaterial() { }

    [SchemaInfo("GSAMaterial", "Creates a Speckle structural material for GSA", "GSA", "Materials")]
    public GSAMaterial(int nativeId, string name, MaterialType type, string grade = null, string designCode = null, string codeYear = null, double strength = 0, double elasticModulus = 0, double poissonsRatio = 0, double shearModulus = 0, double rho = 0, double alpha = 0, double dampingRatio = 0, double cost = 0, string colour = null)
    {
      this.nativeId = nativeId;
      this.name = name;
      this.grade = grade;
      this.type = type;
      this.designCode = designCode;
      this.codeYear = codeYear;
      this.strength = strength;
      this.elasticModulus = elasticModulus;
      this.poissonsRatio = poissonsRatio;
      this.shearModulus = shearModulus;
      this.density = rho;
      this.thermalExpansivity = alpha;
      this.dampingRatio = dampingRatio;
      this.cost = cost;
      this.colour = colour;
    }
  }

  public class GSAConcrete : Concrete
  {
    public int nativeId { get; set; }
    public string colour { get; set; }
    public GSAConcrete() { }

    [SchemaInfo("GSAConcrete", "Creates a Speckle structural concrete material for GSA", "GSA", "Materials")]
    public GSAConcrete(int nativeId, string name, string grade = null, string designCode = null, string codeYear = null, double elasticModulus = 0, double compressiveStrength = 0, double tensileStrength = 0, double flexuralStrength = 0, double maxCompressiveStrain = 0, double maxTensileStrain = 0, double maxAggregateSize = 0, bool lightweight = false, double poissonsRatio = 0, double shearModulus = 0, double density = 0, double alpha = 0, double dampingRatio = 0, double cost = 0, string colour = null)
    {
      this.nativeId = nativeId;
      this.name = name;
      this.grade = grade;
      this.type = MaterialType.Concrete;
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
      this.thermalExpansivity = thermalExpansivity;
      this.dampingRatio = dampingRatio;
      this.cost = cost;
      this.colour = colour;
    }

  }

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

  public class GSASteel : Steel
  {
    public int nativeId { get; set; }
    public string colour { get; set; }
    public GSASteel() { }

    [SchemaInfo("GSASteel", "Creates a Speckle structural material for steel (to be used in structural analysis models)", "Structural", "Materials")]
    public GSASteel(int nativeId, string name, string grade = null, string designCode = null, string codeYear = null, double elasticModulus = 0, double yieldStrength = 0, double ultimateStrength = 0, double maxStrain = 0, double poissonsRatio = 0, double shearModulus = 0, double density = 0, double alpha = 0, double dampingRatio = 0, double cost = 0, string colour = null)
    {
      this.nativeId = nativeId;
      this.name = name;
      this.grade = grade;
      this.type = MaterialType.Concrete;
      this.designCode = designCode;
      this.codeYear = codeYear;
      this.elasticModulus = elasticModulus;
      this.yieldStrength = yieldStrength;
      this.ultimateStrength = ultimateStrength;
      this.maxStrain = maxStrain;
      this.poissonsRatio = poissonsRatio;
      this.shearModulus = shearModulus;
      this.density = density;
      this.thermalExpansivity = thermalExpansivity;
      this.dampingRatio = dampingRatio;
      this.cost = cost;
      this.colour = colour;
    }
  }

  // FROM GWA
  //public string Name { get => name; set { name = value; } }
  //public GsaMat Mat;
  //public double? Fy;
  //public double? Fu;
  //public double? EpsP; 
  //public double? Eh; //
}
