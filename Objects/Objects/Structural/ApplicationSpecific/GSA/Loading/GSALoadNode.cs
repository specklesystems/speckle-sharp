using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Loading;

public class GSALoadNode : LoadNode
{
  public GSALoadNode() { }

  [SchemaInfo("GSALoadNode", "Creates a Speckle node load for GSA", "GSA", "Loading")]
  public GSALoadNode(
    int nativeId,
    string name,
    LoadCase loadCase,
    List<GSANode> nodes,
    LoadDirection direction,
    double value
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.loadCase = loadCase;
    List<Node> baseNodes = nodes.ConvertAll(x => (Node)x);
    this.nodes = baseNodes;
    this.direction = direction;
    this.value = value;
  }

  [SchemaInfo(
    "GSALoadNode (user-defined axis)",
    "Creates a Speckle node load (user-defined axis) for GSA",
    "GSA",
    "Loading"
  )]
  public GSALoadNode(
    int nativeId,
    string name,
    LoadCase loadCase,
    List<Node> nodes,
    Axis loadAxis,
    LoadDirection direction,
    double value
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.loadCase = loadCase;
    this.nodes = nodes;
    this.loadAxis = loadAxis;
    this.direction = direction;
    this.value = value;
  }

  public int nativeId { get; set; }
}
