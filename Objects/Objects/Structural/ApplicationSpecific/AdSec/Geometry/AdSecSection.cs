using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.AdSec.Properties.Reinforcement.Groups;
using Objects.Structural.AdSec.Properties;

namespace Objects.Structural.AdSec.Geometry
{
    public class AdSecSection : Base
    {
        public string name { get; set; }

        [DetachProperty]
        public SectionProfile profile { get; set; }

        [DetachProperty]
        public AdSecSectionProperties properties { get; set; }

        [DetachProperty]
        public List<AdSecReinforcementGroup> reinforcement { get; set; } = new List<AdSecReinforcementGroup>();

        [DetachProperty]
        public Material material { get; set; }

        public AdSecSection() { }

        [SchemaInfo("AdSecSection", "Creates a Speckle section for AdSec", "AdSec", "Geometry")]
        public AdSecSection(string name, Material material, SectionProfile profile, AdSecSectionProperties properties, List<AdSecReinforcementGroup> reinforcement = null)
        {
            this.name = name;
            this.material = material;
            this.profile = profile;
            this.properties = properties;
            this.reinforcement = reinforcement;            
        }
    }
}
