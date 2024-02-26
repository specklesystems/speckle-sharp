using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Geometry;

public class GSAMember1D : Element1D
{
  public GSAMember1D() { }

  [SchemaInfo(
    "GSAMember1D (from local axis)",
    "Creates a Speckle structural 1D member for GSA (from local axis)",
    "GSA",
    "Geometry"
  )]
  public GSAMember1D(
    int nativeId,
    Line baseLine,
    Property1D property,
    ElementType1D type,
    Restraint end1Releases,
    Restraint end2Releases,
    Vector end1Offset,
    Vector end2Offset,
    Plane localAxis
  )
  {
    this.nativeId = nativeId;
    this.baseLine = baseLine;
    this.property = property;
    this.type = type;
    this.end1Releases = end1Releases;
    this.end2Releases = end2Releases;
    this.end1Offset = end1Offset;
    this.end2Offset = end2Offset;
    this.localAxis = localAxis;
  }

  [SchemaInfo(
    "GSAMember1D (from orientation node and angle)",
    "Creates a Speckle structural 1D member for GSA (from orientation node and angle)",
    "GSA",
    "Geometry"
  )]
  public GSAMember1D(
    int nativeId,
    Line baseLine,
    Property1D property,
    ElementType1D type,
    Restraint end1Releases,
    Restraint end2Releases,
    Vector end1Offset,
    Vector end2Offset,
    GSANode orientationNode,
    double orientationAngle
  )
  {
    this.nativeId = nativeId;
    this.baseLine = baseLine;
    this.property = property;
    this.type = type;
    this.end1Releases = end1Releases;
    this.end2Releases = end2Releases;
    this.end1Offset = end1Offset;
    this.end2Offset = end2Offset;
    this.orientationNode = orientationNode;
    this.orientationAngle = orientationAngle;
  }

  public int nativeId { get; set; }
  public int group { get; set; }
  public string colour { get; set; }
  public bool isDummy { get; set; }
  public bool intersectsWithOthers { get; set; }
  public double targetMeshSize { get; set; }
}
