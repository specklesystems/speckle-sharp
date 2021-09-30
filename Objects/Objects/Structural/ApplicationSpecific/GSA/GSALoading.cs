using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.GSA.Geometry;
using System;

namespace Objects.Structural.GSA.Loading
{
    public class GSALoadCase : LoadCase
    {
        public int nativeId { get; set; }
        public GSALoadCase() { }

        [SchemaInfo("GSALoadCase", "Creates a Speckle structural load case for GSA", "GSA", "Loading")]
        public GSALoadCase(int nativeId, string name, LoadType loadType, string source = null, ActionType actionType = ActionType.None, string description = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadType = loadType;
            this.source = source;
            this.actionType = actionType;
            this.description = description;
        }
    }

    public class GSALoadCombination : LoadCombination 
    {
        public int nativeId { get; set; }
        public GSALoadCombination() { }

        public GSALoadCombination(int nativeId, string name,
            [SchemaParamInfo("A dictionary with key/value pairs to map a load factor (value) to a load case (key)")] Dictionary<string, double> caseFactors)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.caseFactors = caseFactors;
            this.nativeId = nativeId;
        }

        [SchemaInfo("GSALoadCombination", "Creates a Speckle load combination for GSA", "GSA", "Loading")]
        public GSALoadCombination(int nativeId, string name,
            [SchemaParamInfo("A list of load cases")] List<LoadCase> loadCases,
            [SchemaParamInfo("A list of load factors (to be mapped to provided load cases)")] List<double> loadFactors)
        {
            this.nativeId = nativeId;
            this.name = name;

            if (loadCases.Count != loadFactors.Count)
                throw new ArgumentException("Number of load cases provided does not match number of load factors provided");

            var caseFactorsDict = new Dictionary<string, double> { };
            for (int i = 0; i < loadCases.Count; i++)
                caseFactorsDict[loadCases[i].name] = loadFactors[i];
            this.caseFactors = caseFactorsDict;

            this.nativeId = nativeId;
        }
    }

    public class GSANodeLoad : NodeLoad
    {
        public int nativeId { get; set; }
        public GSANodeLoad() { }

        [SchemaInfo("GSANodeLoad", "Creates a Speckle node load for GSA", "GSA", "Loading")]
        public GSANodeLoad(int nativeId, string name, LoadCase loadCase, List<GSANode> nodes, LoadDirection direction, double value)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadCase = loadCase;
            List<Node> baseNodes = nodes.ConvertAll(x => (Node)x);
            this.nodes = baseNodes;
            this.direction = direction;
            this.value = value;            
        }

        [SchemaInfo("GSANodeLoad (user-defined axis)", "Creates a Speckle node load (user-defined axis) for GSA", "GSA", "Loading")]
        public GSANodeLoad(int nativeId, string name, LoadCase loadCase, List<Node> nodes, Axis loadAxis, LoadDirection direction, double value)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadCase = loadCase;
            this.nodes = nodes;
            this.loadAxis = loadAxis;
            this.direction = direction;
            this.value = value;
        }
    }

    public class GSAGravityLoad : GravityLoad
    {
        public int nativeId { get; set; }
        public GSAGravityLoad() { }

        [SchemaInfo("GSAGravityLoad", "Creates a Speckle structural gravity load (applied to all nodes and elements) for GSA", "GSA", "Loading")]
        public GSAGravityLoad(int nativeId, string name, LoadCase loadCase, Vector gravityFactors = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;            
        }

        [SchemaInfo("GSAGravityLoad (specified elements)", "Creates a Speckle structural gravity load (applied to specified elements) for GSA", "GSA", "Loading")]
        public GSAGravityLoad(int nativeId, string name, LoadCase loadCase, List<Base> elements, Vector gravityFactors = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.elements = elements;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }

        [SchemaInfo("GSAGravityLoad (specified elements and nodes)", "Creates a Speckle structural gravity load (applied to specified nodes and elements) for GSA", "GSA", "Loading")]
        public GSAGravityLoad(int nativeId, string name, LoadCase loadCase, List<Base> elements, List<Base> nodes, Vector gravityFactors = null, string nativedId = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.elements = elements;
            this.nodes = nodes;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }
    }

    public class GSAFaceLoad : FaceLoad
    {
        public int nativeId { get; set; }
        public GSAFaceLoad() { }

        [SchemaInfo("GSAFaceLoad", "Creates a Speckle structural face (2D elem/member) load for GSA", "GSA", "Loading")]
        public GSAFaceLoad(int nativeId, LoadCase loadCase, List<Base> elements, AreaLoadType loadType, LoadDirection direction, LoadAxisType loadAxisType = LoadAxisType.Global,
            [SchemaParamInfo("A list that represents load magnitude (number of values varies based on load type - Uniform: 1, Variable: 4 (corner nodes), Point: 1)")] List<double> values = null,
            [SchemaParamInfo("A list that represents load locations (number of values varies based on load type - Uniform: null, Variable: null, Point: 2)")] List<double> positions = null,
            bool isProjected = false)
        {
            this.nativeId = nativeId;
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

    public class GSABeamLoad : BeamLoad
    {
        public int nativeId { get; set; }
        public GSABeamLoad() { }

        [SchemaInfo("GSABeamLoad", "Creates a Speckle structural beam (1D elem/member) load for GSA", "GSA", "Loading")]
        public GSABeamLoad(int nativeId, LoadCase loadCase, List<Base> elements, BeamLoadType loadType, LoadDirection direction, LoadAxisType loadAxisType = LoadAxisType.Global,
            [SchemaParamInfo("A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)")] List<double> values = null,
            [SchemaParamInfo("A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)")] List<double> positions = null,
            bool isProjected = false)
        {
            this.nativeId = nativeId;
            this.loadCase = loadCase;
            this.elements = elements;
            this.loadType = loadType;
            this.direction = direction;
            this.loadAxisType = loadAxisType;
            this.values = values;
            this.positions = positions;
            this.isProjected = isProjected;
        }

        [SchemaInfo("GSABeamLoad (user-defined axis)", "Creates a Speckle structural beam (1D elem/member) load (specified for a user-defined axis) for GSA", "GSA", "Loading")]
        public GSABeamLoad(int nativeId, LoadCase loadCase, List<Base> elements, BeamLoadType loadType, LoadDirection direction, Axis loadAxis,
            [SchemaParamInfo("A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)")] List<double> values = null,
            [SchemaParamInfo("A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)")] List<double> positions = null,
            bool isProjected = false)
        {
            this.nativeId = nativeId;
            this.loadCase = loadCase;
            this.elements = elements;
            this.loadType = loadType;
            this.direction = direction;
            this.values = values;
            this.positions = positions;
            this.isProjected = isProjected;
            this.nativeId = nativeId;
        }
    }
}
