using Objects.Structural.GSA.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Bridge;

public class GSAInfluenceBeam : GSAInfluence
{
  public GSAInfluenceBeam() { }

  [SchemaInfo(
    "GSAInfluenceBeam",
    "Creates a Speckle structural beam influence effect for GSA (for an influence analysis)",
    "GSA",
    "Bridge"
  )]
  public GSAInfluenceBeam(
    int nativeId,
    string name,
    double factor,
    InfluenceType type,
    LoadDirection direction,
    GSAElement1D element,
    double position
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.factor = factor;
    this.type = type;
    this.direction = direction;
    this.element = element;
    this.position = position;
  }

  [DetachProperty]
  public GSAElement1D element { get; set; }

  public double position { get; set; }
}
