using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Materials
{
    public class Steel : Material
    {
        public double yieldStrength { get; set; }
        public double ultimateStrength { get; set; }
        public double density { get; set; }
        public double youngsModulus { get; set; }
        public double shearModulus { get; set; }
        public double poissonsRatio { get; set; }
        public double thermalExpansivity { get; set; }
        public double maxStrain { get; set; }
        public Steel() { }

        [SchemaInfo("Steel", "Creates a Speckle structural material for steel (to be used in structural analysis models)", "Structural", "Materials")]
        public Steel(string name, string grade = null, double yieldStrength = 0, double ultimateStrength = 0, double density = 0, double youngsModulus = 0, double shearModulus = 0, double poissonsRatio = 0, double thermalExpansivity = 0) 
        {
            this.name = name;
            this.grade = grade;
            this.type = MaterialType.Steel;
            this.yieldStrength = yieldStrength;
            this.ultimateStrength = ultimateStrength;
            this.density = density;
            this.youngsModulus = youngsModulus;
            this.shearModulus = shearModulus;
            this.poissonsRatio = poissonsRatio;
            this.thermalExpansivity = thermalExpansivity;
        }
    }
}
