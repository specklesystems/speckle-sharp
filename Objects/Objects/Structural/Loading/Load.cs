using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Loading
{
    public class Load : Base
    {
        public string name { get; set; }

        [DetachProperty]
        public LoadCase loadCase { get; set; }
        public string units { get; set; }
        public Load() { }

        [SchemaInfo("Load", "Creates a Speckle structural load", "Structural", "Loading")]
        public Load(string name, LoadCase loadCase)
        {
            this.name = name;
            this.loadCase = loadCase;
        }
    }
}
