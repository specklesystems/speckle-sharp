using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.Materials;

namespace Objects.Structural.GSA.Properties
{
    public class Property1D : Structural.Properties.Property1D
    {
        public int nativeId { get; set; }
        public Material designMaterial { get; set; }
        public double additionalMass { get; set; }
        public double? cost { get; set; }        
        public int? poolRef { get; set; }
        public string colour { get; set; }
        public Property1D() { }        

        [SchemaInfo("GSAProperty1D", "Creates a Speckle structural 1D element property for GSA", "GSA", "Properties")]
        public Property1D(int nativeId, string name, Material material, SectionProfile profile, double cost = 0, double additionalMass = 0)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.material = material;
            this.profile = profile;
            this.cost = cost;
            this.additionalMass = additionalMass;
        }
    }
}
