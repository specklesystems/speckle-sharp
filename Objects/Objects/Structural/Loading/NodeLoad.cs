using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;

namespace Objects.Structural.Loading
{
    public class NodeLoad : Load
    {
        [DetachProperty]
        public List<Node> nodes {get; set;}

        [DetachProperty]
        public Axis loadAxis { get; set; }
        public LoadDirection direction { get; set; }
        public double value { get; set; } //a force or a moment or a displacement (translation or rotation)

        public NodeLoad() { }

        [SchemaInfo("NodeLoad", "Creates a Speckle node load", "Structural", "Loading")]
        public NodeLoad(string name, LoadCase loadCase, List<Node> nodes, LoadDirection direction, double value)
        {
            this.name = name;
            this.loadCase = loadCase;
            this.nodes = nodes;
            this.direction = direction;
            this.value = value;
        }

        [SchemaInfo("NodeLoad (user-defined axis)", "Creates a Speckle node load (user-defined axis)", "Structural", "Loading")]
        public NodeLoad(string name, LoadCase loadCase, List<Node> nodes, Axis loadAxis, LoadDirection direction, double value)
        {
            this.name = name;
            this.loadCase = loadCase;
            this.nodes = nodes;
            this.loadAxis = loadAxis;
            this.direction = direction;
            this.value = value;
        }
    }
}
