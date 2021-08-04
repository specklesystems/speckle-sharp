using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Loading
{
    public class AreaLoad : Load
    {
        [DetachProperty]
        [Chunkable(5000)]
        public List<Base> elements { get; set; } //element list, make this chunkable, make this detachable too?
        public AreaLoadType loadType { get; set; }
        public LoadAxisType loadAxis {get; set;}

        [DetachProperty]
        public Plane userDefinedAxis { get; set; }
        public Vector value { get; set; }
        public AreaLoad() { }

        //[SchemaInfo("AreaLoad", "Creates a Speckle structural area (2D elem/member) load")]
        public AreaLoad(LoadCase loadCase, List<Base> elements, Vector value)
        {
            this.loadCase = loadCase;
            this.elements = elements;
            this.value = value;
        }
    }
}
