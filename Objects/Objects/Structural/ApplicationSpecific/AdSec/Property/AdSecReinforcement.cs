using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;

namespace Objects.Structural.AdSec.Properties.Reinforcement
{    
    public class AdSecBarBundle : Base
    {
        public string name { get; set; }
        public int countPerBundle { get; set; }
        public double diameter { get; set; }
        public Material material { get; set; }
        public AdSecBarBundle() { }

        [SchemaInfo("AdSecBarBundle", "Creates a Speckle bar bundle object for AdSec", "AdSec", "Reinforcement")]
        public AdSecBarBundle(string name, int countPerBundle, double diameter, Material material)
        {
            this.name = name;
            this.countPerBundle = countPerBundle;
            this.diameter = diameter;
            this.material = material;
        }
    }

    public class AdSecPoint : Base
    {
        public string name { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public AdSecPoint() { }

        [SchemaInfo("AdSecPoint", "Creates a Speckle reinforcement point object for AdSec", "AdSec", "Reinforcement")]
        public AdSecPoint(double y, double z)
        {
            this.Y = y;
            this.Z = z;
        }
    }
}
