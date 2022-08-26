using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Materials
{
  public class Timber : Material
  {
    //missing timber-specific properties? parallel to grain, perpendicular to grain
    public string species { get; set; }
    public Timber() { }

    [SchemaInfo("Timber", "Creates a Speckle structural material for timber (to be used in structural analysis models)", "Structural", "Materials")]
    public Timber(string name, string species = null, string grade = null, string designCode = null, string codeYear = null, double strength = 0, double elasticModulus = 0, double poissonsRatio = 0, double shearModulus = 0, double density = 0, double thermalExpansivity = 0, double dampingRatio = 0)
    {
      this.name = name;
      this.grade = grade;
      this.species = species;
      this.materialType = MaterialType.Timber;
      this.designCode = designCode;
      this.codeYear = codeYear;
      this.strength = strength;
      this.elasticModulus = elasticModulus;
      this.poissonsRatio = poissonsRatio;
      this.shearModulus = shearModulus;
      this.density = density;
      this.thermalExpansivity = thermalExpansivity;
      this.dampingRatio = dampingRatio;
    }
  }
}
