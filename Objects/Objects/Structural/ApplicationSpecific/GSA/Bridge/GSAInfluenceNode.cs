using Speckle.Core.Models;
using Speckle.Core.Kits;
using Objects.Structural.Loading;
using Objects.Structural.Geometry;

namespace Objects.Structural.GSA.Bridge
{
  public class GSAInfluenceNode : GSAInfluence
  {
    [DetachProperty]
    public Node node { get; set; }

    [DetachProperty]
    public Axis axis { get; set; }
    public GSAInfluenceNode() { }

    [SchemaInfo("GSAInfluenceBeam", "Creates a Speckle structural node influence effect for GSA (for an influence analysis)", "GSA", "Bridge")]
    public GSAInfluenceNode(int nativeId, string name, double factor, InfluenceType type, LoadDirection direction, Node node, Axis axis)
    {
      this.nativeId = nativeId;
      this.name = name;
      this.factor = factor;
      this.type = type;
      this.direction = direction;
      this.node = node;
      this.axis = axis;
    }
  }
}
