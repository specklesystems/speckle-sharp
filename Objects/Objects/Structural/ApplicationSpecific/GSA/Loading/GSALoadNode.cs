using Speckle.Core.Kits;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.GSA.Geometry;
using GSANode = Objects.Structural.GSA.Geometry.GSANode;

namespace Objects.Structural.GSA.Loading
{
  public class GSALoadNode : LoadNode
  {
    public int nativeId { get; set; }
    public GSALoadNode() { }

    [SchemaInfo("GSALoadNode", "Creates a Speckle node load for GSA", "GSA", "Loading")]
    public GSALoadNode(int nativeId, string name, Structural.Loading.LoadCase loadCase, List<GSANode> nodes, LoadDirection direction, double value)
    {
      this.nativeId = nativeId;
      this.name = name;
      this.loadCase = loadCase;
      List<Structural.Geometry.Node> baseNodes = nodes.ConvertAll(x => (Structural.Geometry.Node)x);
      this.nodes = baseNodes;
      this.direction = direction;
      this.value = value;
    }

    [SchemaInfo("GSALoadNode (user-defined axis)", "Creates a Speckle node load (user-defined axis) for GSA", "GSA", "Loading")]
    public GSALoadNode(int nativeId, string name, Structural.Loading.LoadCase loadCase, List<Structural.Geometry.Node> nodes, Axis loadAxis, LoadDirection direction, double value)
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





}
