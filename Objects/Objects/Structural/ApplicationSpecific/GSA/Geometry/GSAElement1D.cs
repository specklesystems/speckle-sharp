using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Geometry;

public class GSAElement1D : Element1D
{
  public GSAElement1D() { }

  [SchemaInfo(
    "GSAElement1D (from local axis)",
    "Creates a Speckle structural 1D element for GSA (from local axis)",
    "GSA",
    "Geometry"
  )]
  public GSAElement1D(
    int nativeId,
    Line baseLine,
    Property1D property,
    ElementType1D type,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end2Offset = null,
    Plane? localAxis = null
  )
  {
    this.nativeId = nativeId;
    this.baseLine = baseLine;
    this.property = property;
    this.type = type;
    this.end1Releases = end1Releases ?? new Restraint("FFFFFF");
    this.end2Releases = end2Releases ?? new Restraint("FFFFFF");
    this.end1Offset = end1Offset ?? new Vector(0, 0, 0);
    this.end2Offset = end2Offset ?? new Vector(0, 0, 0);
    this.localAxis = localAxis;
  }

  [SchemaInfo(
    "GSAElement1D (from orientation node and angle)",
    "Creates a Speckle structural 1D element for GSA (from orientation node and angle)",
    "GSA",
    "Geometry"
  )]
  public GSAElement1D(
    int nativeId,
    Line baseLine,
    Property1D property,
    ElementType1D type,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end2Offset = null,
    Node? orientationNode = null,
    double orientationAngle = 0
  )
  {
    this.nativeId = nativeId;
    this.baseLine = baseLine;
    this.property = property;
    this.type = type;
    this.end1Releases = end1Releases ?? new Restraint("FFFFFF");
    this.end2Releases = end2Releases ?? new Restraint("FFFFFF");
    this.end1Offset = end1Offset ?? new Vector(0, 0, 0);
    this.end2Offset = end2Offset ?? new Vector(0, 0, 0);
    this.orientationNode = orientationNode;
    this.orientationAngle = orientationAngle;
  }

  public int nativeId { get; set; } //equiv to num record of gwa keyword
  public int group { get; set; }
  public string colour { get; set; }
  public string action { get; set; }
  public bool isDummy { get; set; }
}
