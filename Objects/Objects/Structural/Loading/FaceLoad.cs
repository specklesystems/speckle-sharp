using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;

namespace Objects.Structural.Loading
{
    public class FaceLoad : Load
    {
        [DetachProperty]
        [Chunkable(5000)]
        public List<Base> elements { get; set; } //element list, make this chunkable, make this detachable too?
        public AreaLoadType loadType { get; set; }
        public LoadDirection direction { get; set; }

        [DetachProperty]
        public Axis loadAxis { get; set; }
        public LoadAxisType loadAxisType { get; set; }
        public bool isProjected { get; set; }
        public List<double> values { get; set; }
        public List<double> positions { get; set; }
        public FaceLoad() { }

        [SchemaInfo("FaceLoad", "Creates a Speckle structural face (2D elem/member) load", "Structural", "Loading")]
        public FaceLoad(LoadCase loadCase, List<Base> elements, AreaLoadType loadType, LoadDirection direction, LoadAxisType loadAxisType = LoadAxisType.Global, 
            [SchemaParamInfo("A list that represents load magnitude (number of values varies based on load type - Uniform: 1, Variable: 4 (corner nodes), Point: 1)")] List<double> values = null,
            [SchemaParamInfo("A list that represents load locations (number of values varies based on load type - Uniform: null, Variable: null, Point: 2)")] List<double> positions = null, 
            bool isProjected = false)
        {
            this.loadCase = loadCase;
            this.elements = elements;
            this.loadType = loadType;
            this.direction = direction;
            this.loadAxisType = loadAxisType;
            this.values = values;
            this.positions = positions;
            this.isProjected = isProjected;
        }
    }
}
