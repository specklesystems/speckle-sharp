using Objects.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Geometry;

public class Node : Base
{
  public Node() { }

  public Node(Point basePoint)
  {
    this.basePoint = basePoint;
  }

  [SchemaInfo(
    "Node with properties",
    "Creates a Speckle structural node with spring, mass and/or damper properties",
    "Structural",
    "Geometry"
  )]
  public Node(
    Point basePoint,
    string? name = null,
    [SchemaParamInfo("If null, restraint condition defaults to free/fully released")] Restraint? restraint = null,
    [SchemaParamInfo(
      "If null, axis defaults to world xy (z axis defines the vertical direction, positive direction is up)"
    )]
      Axis? constraintAxis = null,
    PropertySpring? springProperty = null,
    PropertyMass? massProperty = null,
    PropertyDamper? damperProperty = null
  )
  {
    this.basePoint = basePoint;
    this.name = name;
    this.restraint = restraint ?? new Restraint("RRRRRR");
    this.constraintAxis =
      constraintAxis
      ?? new Axis(
        "Global",
        AxisType.Cartesian,
        new Plane(new Point(0, 0), new Vector(0, 0, 1), new Vector(1, 0, 0), new Vector(0, 1, 0))
      );
    this.springProperty = springProperty;
    this.massProperty = massProperty;
    this.damperProperty = damperProperty;
  }

  //public int nativeId { get; set; } //equivalent to num record in GWA keyword, can be used as a unique identifier for other software
  public string? name { get; set; }
  public Point basePoint { get; set; }

  [DetachProperty]
  public Axis? constraintAxis { get; set; } // can be detachable? ex. a user-specified axis

  [DetachProperty]
  public Restraint? restraint { get; set; } // can be detachable? ex. reuse pinned support condition

  [DetachProperty]
  public PropertySpring? springProperty { get; set; }

  [DetachProperty]
  public PropertyMass? massProperty { get; set; }

  [DetachProperty]
  public PropertyDamper? damperProperty { get; set; }

  public string units { get; set; } = Units.None;
}
