using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Bridge;

public class GSAInfluenceNode : GSAInfluence
{
  public GSAInfluenceNode() { }

  [SchemaInfo(
    "GSAInfluenceBeam",
    "Creates a Speckle structural node influence effect for GSA (for an influence analysis)",
    "GSA",
    "Bridge"
  )]
  public GSAInfluenceNode(
    int nativeId,
    string name,
    double factor,
    InfluenceType type,
    LoadDirection direction,
    Node node,
    Axis axis
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.factor = factor;
    this.type = type;
    this.direction = direction;
    this.node = node;
    this.axis = axis;
  }

  [DetachProperty]
  public Node node { get; set; }

  [DetachProperty]
  public Axis axis { get; set; }
}
