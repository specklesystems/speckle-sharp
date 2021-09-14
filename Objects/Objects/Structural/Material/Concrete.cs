using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Materials
{
    public class Concrete : Material 
    {
        public double compressiveStrength { get; set; } //specify compressive
        public double density { get; set; }
        public double youngsModulus { get; set; }
        public double shearModulus { get; set; }
        public double poissonsRatio { get; set; }
        public double thermalExpansivity { get; set; }
        public double maxStrain { get; set; }
        public double maxAggregateSize { get; set; }
        public double tensileStrength { get; set; } //design calc impacts
        public double flexuralStrength { get; set; } //design calc impacts

        public Concrete() { }

        [SchemaInfo("Concrete", "Creates a Speckle structural material for concrete (to be used in structural analysis models)", "Structural", "Materials")]
        public Concrete(string name, string grade = null, double compressiveStrength = 0, double density = 0, double youngsModulus = 0, double shearModulus = 0, double poissonsRatio = 0, double thermalExpansivity = 0, double tensileStrength = 0, double flexuralStrength = 0)
        {
            this.name = name;
            this.grade = grade;
            this.type = MaterialType.Concrete;
            this.compressiveStrength = compressiveStrength;
            this.density = density;
            this.youngsModulus = youngsModulus;
            this.shearModulus = shearModulus;
            this.poissonsRatio = poissonsRatio;
            this.thermalExpansivity = thermalExpansivity;
            this.tensileStrength = tensileStrength;
            this.flexuralStrength = flexuralStrength;
        }
    }
}
