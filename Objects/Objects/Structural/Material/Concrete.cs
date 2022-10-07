using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Materials
{
  public class Concrete : StructuralMaterial
  {
    public double compressiveStrength { get; set; } //forgo using "strength" property in Material class
    public double tensileStrength { get; set; } //design calc impacts
    public double flexuralStrength { get; set; } //design calc impacts
    public double maxCompressiveStrain { get; set; } //failure strain
    public double maxTensileStrain { get; set; }
    public double maxAggregateSize { get; set; }
    public bool lightweight { get; set; } //whether or not it's a lightweight concrete

    public Concrete() { }

    [SchemaInfo("Concrete", "Creates a Speckle structural material for concrete (to be used in structural analysis models)", "Structural", "Materials")]
    public Concrete(string name, string grade = null, string designCode = null, string codeYear = null, double elasticModulus = 0, double compressiveStrength = 0, double tensileStrength = 0, double flexuralStrength = 0, double maxCompressiveStrain = 0, double maxTensileStrain = 0, double maxAggregateSize = 0, bool lightweight = false, double poissonsRatio = 0, double shearModulus = 0, double density = 0, double thermalExpansivity = 0, double dampingRatio = 0)
    {
      this.name = name;
      this.grade = grade;
      this.materialType = MaterialType.Concrete;
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
    }
  }
}
