using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.GSA.Geometry;

public class GSANode : Node
{
  public GSANode() { }

  /// <summary>
  /// SchemaBuilder constructor for a GSA node
  /// </summary>
  /// <param name="basePoint"></param>
  /// <param name="restraint"></param>
  /// <param name="constraintAxis"></param>
  /// <param name="springProperty"></param>
  /// <param name="massProperty"></param>
  /// <param name="damperProperty"></param>
  /// <param name="localElementSize"></param>
  [SchemaInfo("GSANode", "Creates a Speckle structural node for GSA", "GSA", "Geometry")]
  public GSANode(
    int nativeId,
    Point basePoint,
    Restraint restraint,
    Axis constraintAxis = null,
    PropertySpring springProperty = null,
    PropertyMass massProperty = null,
    PropertyDamper damperProperty = null,
    double localElementSize = 0,
    string colour = "NO_RGB"
  )
  {
    this.nativeId = nativeId;
    this.basePoint = basePoint;
    this.restraint = restraint;
    this.constraintAxis =
      constraintAxis == null
        ? new Axis(
          "Global",
          AxisType.Cartesian,
          new Plane(new Point(0, 0), new Vector(0, 0, 1), new Vector(1, 0, 0), new Vector(0, 1, 0))
        )
        : constraintAxis;
    this.springProperty = springProperty;
    this.massProperty = massProperty;
    this.damperProperty = damperProperty;
    this.localElementSize = localElementSize;
    this.colour = colour;
  }

  public int nativeId { get; set; }
  public double localElementSize { get; set; }
  public string colour { get; set; }
}
