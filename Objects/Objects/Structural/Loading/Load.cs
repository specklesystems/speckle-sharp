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
        public LoadPattern loadCase { get; set; }
        public string units { get; set; }
        public Load() { }

        public Load(string name, LoadPattern loadCase)
        {
            this.name = name;
            this.loadCase = loadCase;
        }
    }
}
