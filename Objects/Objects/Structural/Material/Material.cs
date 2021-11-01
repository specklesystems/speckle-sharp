using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Materials
{
    public class Material : Base
    {
        public string name { get; set; }
        public string grade { get; set; } //ex. 350W(G40.21 Plate), could be set in name too
        public MaterialType materialType { get; set; }
        public string designCode { get; set; }
        public string codeYear { get; set; }
        public double strength { get; set; } 
        public double elasticModulus { get; set; } // E
        public double poissonsRatio { get; set; } // nu
        public double shearModulus { get; set; } // G
        public double density { get; set; } // rho
        public double thermalExpansivity { get; set; } // alpha, thermal coefficient of expansion
        public double dampingRatio { get; set; } // zeta, material damping fraction
        public double cost { get; set; } // material rate (ie. $/weight)
        public double materialSafetyFactor { get; set; } //resistance factor

        // add carbon/environmental parameters?

        public Material() { }

        [SchemaInfo("Material", "Creates a Speckle structural material", "Structural", "Materials")]
        public Material(string name, MaterialType type, string grade = null, string designCode = null, string codeYear = null)
        {
            this.name = name;
            this.materialType = type;
            this.grade = grade;
            this.designCode = designCode;
            this.codeYear = codeYear;
        }

        [SchemaInfo("Material (with properties)", "Creates a Speckle structural material with (isotropic) properties", "Structural", "Materials")]
        public Material(string name, MaterialType type, string grade = null, string designCode = null, string codeYear = null, double strength = 0, double elasticModulus = 0, double poissonsRatio = 0, double shearModulus = 0, double rho = 0, double alpha = 0, double dampingRatio = 0, double materialSafetyFactor = 0, double cost = 0)
        {
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
            this.materialSafetyFactor = materialSafetyFactor;
            this.cost = cost;
        }
    }
}
