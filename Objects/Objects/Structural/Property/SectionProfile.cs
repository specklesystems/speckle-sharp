using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;

namespace Objects.Structural.Properties
{
    public class SectionProfile : Base //section description
    {
        public string name { get; set; }
        public string shapeDescription { get; set; }
        public SectionProfile() { }

        [SchemaInfo("SectionProfile", "Creates a Speckle structural 1D element section profile")]
        public SectionProfile(string shapeDescription)
        {
            this.shapeDescription = shapeDescription;
        }

    }
}
