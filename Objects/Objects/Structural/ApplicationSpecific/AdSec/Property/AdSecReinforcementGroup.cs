using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.AdSec.Properties.Reinforcement.Layers;


namespace Objects.Structural.AdSec.Properties.Reinforcement.Groups
{
    public class AdSecReinforcementGroup : Base
    {
        public string name { get; set; }
        public AdSecReinforcementGroup() { }

        [SchemaInfo("AdSecReinforcementGroup", "Creates a Speckle reinforcement group for AdSec", "AdSec", "Reinforcement")]
        public AdSecReinforcementGroup(string name)
        {
            this.name = name;
        }
    }

    public class AdSecSingleBars : AdSecReinforcementGroup
    {
        [DetachProperty]
        public AdSecBarBundle barBundle { get; set; }

        [DetachProperty]
        public List<AdSecPoint> positions { get; set; }
        public AdSecSingleBars() { }

        [SchemaInfo("AdSecSingleBars", "Creates a Speckle reinforcement group with single bars for AdSec", "AdSec", "Reinforcement")]
        public AdSecSingleBars(string name, AdSecBarBundle barBundle, List<AdSecPoint> positions)
        {
            this.name = name;
            this.barBundle = barBundle;
            this.positions = positions;
        }
    }

    public class AdSecLineGroup : AdSecReinforcementGroup
    {
        public AdSecPoint firstBarPosition { get; set; }
        public AdSecPoint finalBarPosition { get; set; }

        [DetachProperty]
        public AdSecReinforcementLayer layer { get; set; }

        public AdSecLineGroup() { }

        [SchemaInfo("AdSecLineGroup", "Creates a Speckle reinforcement group with a line of bars for AdSec", "AdSec", "Reinforcement")]
        public AdSecLineGroup(string name, AdSecPoint firstBarPosition, AdSecPoint finalBarPosition, AdSecReinforcementLayer layer)        
        {
            this.name = name;
            this.firstBarPosition = firstBarPosition;
            this.finalBarPosition = finalBarPosition;
            this.layer = layer;
        }
    }

    public class AdSecCircleGroup : AdSecReinforcementGroup
    {
        public AdSecPoint centreOfTheCircle { get; set; }
        public double radius { get; set; }
        public double angle { get; set; }

        [DetachProperty]
        public AdSecReinforcementLayer layer { get; set; }
        public AdSecCircleGroup() { }

        [SchemaInfo("AdSecCircleGroup", "Creates a Speckle reinforcement group with a circle of bars for AdSec", "AdSec", "Reinforcement")]
        public AdSecCircleGroup(string name, AdSecPoint centreOfTheCircle, double radius, double angle, AdSecReinforcementLayer layer)
        {
            this.name = name;
            this.centreOfTheCircle = centreOfTheCircle;
            this.radius = radius;
            this.angle = angle;
            this.layer = layer;
        }
    }
}
