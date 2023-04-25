using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Geometry;

public class Element1D : Base, IDisplayValue<List<Mesh>>
{
  public Element1D() { }

  public Element1D(Line baseLine)
  {
    this.baseLine = baseLine;
  }

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
  [SchemaInfo(
    "Element1D (from local axis)",
    "Creates a Speckle structural 1D element (from local axis)",
    "Structural",
    "Geometry"
  )]
  public Element1D(
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
    Plane localAxis = null
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
    "Creates a Speckle structural 1D element (from orientation node and angle)",
    "Structural",
    "Geometry"
  )]
  public Element1D(
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
    double orientationAngle = 0
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
  }

  public string name { get; set; } //add unique id as base identifier, name can change too easily
  public Line baseLine { get; set; }

  [DetachProperty]
  public Property1D property { get; set; }

  public ElementType1D type { get; set; }
  public Restraint end1Releases { get; set; }
  public Restraint end2Releases { get; set; }
  public Vector end1Offset { get; set; }
  public Vector end2Offset { get; set; }
  public Node orientationNode { get; set; }
  public double orientationAngle { get; set; }
  public Plane localAxis { get; set; }

  [DetachProperty]
  public Base parent { get; set; } //parent element

  [DetachProperty]
  public Node end1Node { get; set; } //startNode

  [DetachProperty]
  public Node end2Node { get; set; } //endNode

  [DetachProperty]
  public List<Node> topology { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}
