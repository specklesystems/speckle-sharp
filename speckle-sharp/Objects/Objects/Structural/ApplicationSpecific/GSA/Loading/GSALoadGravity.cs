using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Loading;

namespace Objects.Structural.GSA.Loading
{
    public class GSALoadGravity : LoadGravity
    {
        public int nativeId { get; set; }
        public GSALoadGravity() { }

        [SchemaInfo("GSALoadGravity", "Creates a Speckle structural gravity load (applied to all nodes and elements) for GSA", "GSA", "Loading")]
        public GSALoadGravity(int nativeId, string name, Structural.Loading.LoadCase loadCase, Vector gravityFactors = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }

        [SchemaInfo("GSALoadGravity (specified elements)", "Creates a Speckle structural gravity load (applied to specified elements) for GSA", "GSA", "Loading")]
        public GSALoadGravity(int nativeId, string name, Structural.Loading.LoadCase loadCase, List<Base> elements, Vector gravityFactors = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.elements = elements;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }

        [SchemaInfo("GSALoadGravity (specified elements and nodes)", "Creates a Speckle structural gravity load (applied to specified nodes and elements) for GSA", "GSA", "Loading")]
        public GSALoadGravity(int nativeId, string name, Structural.Loading.LoadCase loadCase, List<Base> elements, List<Base> nodes, Vector gravityFactors = null, string nativedId = null)
        {
            this.nativeId = nativeId;
            this.name = name;
            this.elements = elements;
            this.nodes = nodes;
            this.loadCase = loadCase;
            this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
        }
    }





}
