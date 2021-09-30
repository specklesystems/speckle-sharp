using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;

namespace Objects.Structural.Loading
{
    public class BeamLoad : Load
    {
        [DetachProperty]
        [Chunkable(5000)]
        public List<Base> elements { get; set; } //element list, make this chunkable, make this detachable too?
        public BeamLoadType loadType { get; set; }
        public LoadDirection direction { get; set; }

        [DetachProperty]
        public Axis loadAxis { get; set; }
        public LoadAxisType loadAxisType { get; set; }
        public bool isProjected { get; set; }
        public List<double> values { get; set; }
        public List<double> positions { get; set; }
        public BeamLoad() { }

        [SchemaInfo("BeamLoad", "Creates a Speckle structural beam (1D elem/member) load", "Structural", "Loading")]
        public BeamLoad(LoadCase loadCase, List<Base> elements, BeamLoadType loadType, LoadDirection direction, LoadAxisType loadAxisType = LoadAxisType.Global,
            [SchemaParamInfo("A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)")] List<double> values = null,
            [SchemaParamInfo("A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)")] List<double> positions = null,
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

        [SchemaInfo("BeamLoad (user-defined axis)", "Creates a Speckle structural beam (1D elem/member) load (specified for a user-defined axis)", "Structural", "Loading")]
        public BeamLoad(LoadCase loadCase, List<Base> elements, BeamLoadType loadType, LoadDirection direction, Axis loadAxis,
            [SchemaParamInfo("A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)")] List<double> values = null,
            [SchemaParamInfo("A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)")] List<double> positions = null,
            bool isProjected = false)
        {
            this.loadCase = loadCase;
            this.elements = elements;
            this.loadType = loadType;
            this.direction = direction;
            this.values = values;
            this.positions = positions;
            this.isProjected = isProjected;
        }
    }
}
