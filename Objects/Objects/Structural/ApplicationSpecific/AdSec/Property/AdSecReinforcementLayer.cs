using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;


namespace Objects.Structural.AdSec.Properties.Reinforcement.Layers
{
    public class AdSecReinforcementLayer : Base
    {
        public string name { get; set; }
        public AdSecReinforcementLayer() { }

        [SchemaInfo("AdSecReinforcementLayer", "Creates a Speckle reinforcement layer for AdSec", "AdSec", "Reinforcement")]
        public AdSecReinforcementLayer(string name)
        {
            this.name = name;
        }
    }

    public class AdSecLayer : AdSecReinforcementLayer
    {
        [DetachProperty]
        public AdSecBarBundle barBundle { get; set; }
        public AdSecLayer() { }

        [SchemaInfo("AdSecLayer", "Creates a Speckle reinforcement layer based on a bar bundle for AdSec", "AdSec", "Reinforcement")]
        public AdSecLayer(string name, AdSecBarBundle barBundle)
        {
            this.name = name;
            this.barBundle = barBundle;
        }
    }

    public class AdSecLayerByBarCount : AdSecReinforcementLayer
    {
        public int count { get; set; }

        [DetachProperty]
        public AdSecBarBundle barBundle { get; set; }
        public AdSecLayerByBarCount() { }

        [SchemaInfo("AdSecLayerByBarCount", "Creates a Speckle reinforcement layer based on a number of bars for AdSec", "AdSec", "Reinforcement")]
        public AdSecLayerByBarCount(string name, int count, AdSecBarBundle barBundle)
        {
            this.name = name;
            this.count = count;
            this.barBundle = barBundle;
        }
    }
}
