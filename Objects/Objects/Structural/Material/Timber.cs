using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Materials
{
    public class Timber : Material
    {
        public double strength { get; set; }
        public double density { get; set; }
        public double youngsModulus { get; set; }
        public double shearModulus { get; set; }
        public double poissonsRatio { get; set; }
        public double thermalExpansivity { get; set; }

        public Timber() { }

        [SchemaInfo("Timber", "Creates a Speckle structural material for timber (to be used in structural analysis models)", "Structural", "Materials")]
        public Timber(string name, string grade = null, double strength = 0, double density = 0, double youngsModulus = 0, double shearModulus = 0, double poissonsRatio = 0, double thermalExpansivity = 0)
        {
            this.name = name;
            this.type = MaterialType.Timber;
            this.grade = grade;
            this.strength = strength;
            this.density = density;
            this.youngsModulus = youngsModulus;
            this.shearModulus = shearModulus;
            this.poissonsRatio = poissonsRatio;
            this.thermalExpansivity = thermalExpansivity;
        }
    }
}
