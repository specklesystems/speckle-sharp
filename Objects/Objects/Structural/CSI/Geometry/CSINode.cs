using Objects.Geometry;
using Objects.Structural.CSI.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Results;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Geometry;

public class CSINode : Node
{
  [SchemaInfo(
    "Node with properties",
    "Creates a Speckle CSI node with spring, mass and/or damper properties",
    "CSI",
    "Geometry"
  )]
  public CSINode(
    Point basePoint,
    string? name = null,
    [SchemaParamInfo("If null, restraint condition defaults to free/fully released")] Restraint? restraint = null,
    [SchemaParamInfo(
      "If null, axis defaults to world xy (z axis defines the vertical direction, positive direction is up)"
    )]
      Axis? constraintAxis = null,
    CSISpringProperty? springProperty = null,
    PropertyMass? massProperty = null,
    PropertyDamper? damperProperty = null,
    CSIDiaphragm? CSIDiaphragm = null,
    DiaphragmOption DiaphragmOption = DiaphragmOption.FromShellObject
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
    CSISpringProperty = springProperty;
    this.massProperty = massProperty;
    this.damperProperty = damperProperty;
    DiaphragmAssignment = CSIDiaphragm?.name;
    this.DiaphragmOption = DiaphragmOption;
  }

  public CSINode() { }

  [DetachProperty]
  public CSISpringProperty? CSISpringProperty { get; set; }

  public string? DiaphragmAssignment { get; set; }

  public DiaphragmOption DiaphragmOption { get; set; }

  [DetachProperty]
  public AnalyticalResults? AnalysisResults { get; set; }
}
