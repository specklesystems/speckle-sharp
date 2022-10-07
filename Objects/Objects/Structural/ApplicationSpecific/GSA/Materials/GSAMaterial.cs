using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;

namespace Objects.Structural.GSA.Materials
{
    public class GSAMaterial : StructuralMaterial
    {
        public int nativeId { get; set; }
        public string colour { get; set; }

        public GSAMaterial() { }

        [SchemaInfo("GSAMaterial", "Creates a Speckle structural material for GSA", "GSA", "Materials")]
        public GSAMaterial(int nativeId, string name, MaterialType type, string grade = null, string designCode = null, string codeYear = null, double strength = 0, double elasticModulus = 0, double poissonsRatio = 0, double shearModulus = 0, double rho = 0, double alpha = 0, double dampingRatio = 0, double cost = 0, string colour = "NO_RGB")
        {
            this.nativeId = nativeId;
            this.name = name;
            this.grade = grade;
            this.materialType = type;
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
}
