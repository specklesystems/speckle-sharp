using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Structural.Loading
{
    public class GravityLoad : Load
    {
        [DetachProperty]
        [Chunkable(5000)]
        public List<Base> elements { get; set; } 

        [DetachProperty]
        [Chunkable(5000)]
        public List<Base> nodes { get; set; } 
        public Vector gravityFactors { get; set; }
        public GravityLoad(){}

        [SchemaInfo("GravityLoad", "Creates a Speckle structural gravity load (applied to all nodes and elements)", "Structural", "Loading")]
        public GravityLoad(string name, LoadCase loadCase, Vector gravityFactors = null)
        {
            this.name = name;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }

        [SchemaInfo("GravityLoad (specified elements)", "Creates a Speckle structural gravity load (applied to specified elements)", "Structural", "Loading")]
        public GravityLoad(string name, LoadCase loadCase, List<Base> elements, Vector gravityFactors = null)
        {
            this.name = name;
            this.elements = elements;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }

        [SchemaInfo("GravityLoad (specified elements and nodes)", "Creates a Speckle structural gravity load (applied to specified nodes and elements)", "Structural", "Loading")]
        public GravityLoad(string name, LoadCase loadCase, List<Base> elements, List<Base> nodes, Vector gravityFactors = null)
        {
            this.name = name;
            this.elements = elements;
            this.nodes = nodes;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }
    }
}
