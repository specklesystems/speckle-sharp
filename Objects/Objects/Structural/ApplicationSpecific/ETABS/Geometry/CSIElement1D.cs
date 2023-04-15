using Objects.Geometry;
using Objects.Structural.CSI.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Geometry;

public class CSIElement1D : Element1D
{
  /// <summary>
  /// SchemaBuilder constructor for structural 1D element (based on local axis)
  /// </summary>
  /// <param name="baseLine"></param>
  /// <param name="property"></param>
  /// <param name="type"></param>
  /// <param name="name"></param>
  /// <param name="end1Releases"></param>
  /// <param name="end2Releases"></param>
  /// <param name="end1Offset"></param>
  /// <param name="end2Offset"></param>
  /// <param name="localAxis"></param>
  [SchemaInfo("Element1D (from local axis)", "Creates a Speckle CSI 1D element (from local axis)", "CSI", "Geometry")]
  public CSIElement1D(
    Line baseLine,
    Property1D property,
    ElementType1D type,
    string name = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector end2Offset = null,
    Plane localAxis = null,
    CSILinearSpring CSILinearSpring = null,
    [SchemaParamInfo("an Array of 8 values referring to the modifiers as seen in CSI in order")]
      double[] Modifier = null,
    DesignProcedure DesignProcedure = DesignProcedure.NoDesign
  )
  {
    this.baseLine = baseLine;
    this.property = property;
    this.type = type;
    this.name = name;
    this.end1Releases = end1Releases == null ? new Restraint("FFFFFF") : end1Releases;
    this.end2Releases = end2Releases == null ? new Restraint("FFFFFF") : end2Releases;
    this.end1Offset = end1Offset == null ? new Vector(0, 0, 0) : end1Offset;
    this.end2Offset = end2Offset == null ? new Vector(0, 0, 0) : end2Offset;
    this.localAxis = localAxis;
    this.CSILinearSpring = CSILinearSpring;
    this.DesignProcedure = DesignProcedure;
    Modifiers = Modifier;
  }

  /// <summary>
  /// SchemaBuilder constructor for structural 1D element (based on orientation node and angle)
  /// </summary>
  /// <param name="baseLine"></param>
  /// <param name="property"></param>
  /// <param name="type"></param>
  /// <param name="name"></param>
  /// <param name="end1Releases"></param>
  /// <param name="end2Releases"></param>
  /// <param name="end1Offset"></param>
  /// <param name="end2Offset"></param>
  /// <param name="orientationNode"></param>
  /// <param name="orientationAngle"></param>
  [SchemaInfo(
    "Element1D (from orientation node and angle)",
    "Creates a Speckle CSI 1D element (from orientation node and angle)",
    "CSI",
    "Geometry"
  )]
  public CSIElement1D(
    Line baseLine,
    Property1D property,
    ElementType1D type,
    string name = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector end2Offset = null,
    Node orientationNode = null,
    double orientationAngle = 0,
    CSILinearSpring CSILinearSpring = null,
    [SchemaParamInfo("an Array of 8 values referring to the modifiers as seen in CSI in order")]
      double[] Modifier = null,
    DesignProcedure DesignProcedure = DesignProcedure.NoDesign
  )
  {
    this.baseLine = baseLine;
    this.property = property;
    this.type = type;
    this.name = name;
    this.end1Releases = end1Releases == null ? new Restraint("FFFFFF") : end1Releases;
    this.end2Releases = end2Releases == null ? new Restraint("FFFFFF") : end2Releases;
    this.end1Offset = end1Offset == null ? new Vector(0, 0, 0) : end1Offset;
    this.end2Offset = end2Offset == null ? new Vector(0, 0, 0) : end2Offset;
    this.orientationNode = orientationNode;
    this.orientationAngle = orientationAngle;
    this.CSILinearSpring = CSILinearSpring;
    this.DesignProcedure = DesignProcedure;
    Modifiers = Modifier;
  }

  public CSIElement1D() { }

  [DetachProperty]
  public CSILinearSpring CSILinearSpring { get; set; }

  public string PierAssignment { get; set; }
  public string SpandrelAssignment { get; set; }
  public double[] Modifiers { get; set; }
  public DesignProcedure DesignProcedure { get; set; }
}
