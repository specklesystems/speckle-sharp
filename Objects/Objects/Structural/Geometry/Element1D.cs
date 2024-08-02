using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Geometry;

public class Element1D : Base, IDisplayValue<IReadOnlyList<Base>>
{
  public Element1D() { }

  public Element1D(
    Line baseLine,
    Property1D property,
    ElementType1D type,
    string? name,
    string? units,
    Restraint? end1Releases = null,
    Restraint? end2Releases = null,
    Vector? end1Offset = null,
    Vector? end2Offset = null,
    Plane? localAxis = null,
    Node? orientationNode = null,
    double orientationAngle = 0,
    IReadOnlyList<Mesh>? displayValue = null
  )
  {
    this.baseLine = baseLine;
    this.property = property;
    this.type = type;
    this.name = name;
    this.units = units;
    this.end1Releases = end1Releases ?? new Restraint("FFFFFF");
    this.end2Releases = end2Releases ?? new Restraint("FFFFFF");
    this.end1Offset = end1Offset ?? new Vector(0, 0, 0);
    this.end2Offset = end2Offset ?? new Vector(0, 0, 0);
    this.localAxis = localAxis;
    this.orientationNode = orientationNode;
    this.orientationAngle = orientationAngle;
    this.displayValue = ((IReadOnlyList<Base>?)displayValue) ?? new[] { (Base)baseLine };
  }

  public string? name { get; set; } //add unique id as base identifier, name can change too easily
  public Line baseLine { get; set; }

  [DetachProperty]
  public Property1D property { get; set; }

  public ElementType1D type { get; set; }
  public Restraint end1Releases { get; set; }
  public Restraint end2Releases { get; set; }
  public Vector end1Offset { get; set; }
  public Vector end2Offset { get; set; }
  public Node? orientationNode { get; set; }
  public double orientationAngle { get; set; }
  public Plane? localAxis { get; set; }

  [DetachProperty]
  public Base? parent { get; set; } //parent element

  [DetachProperty]
  public Node? end1Node { get; set; } //startNode

  [DetachProperty]
  public Node? end2Node { get; set; } //endNode

  [DetachProperty]
  public List<Node>? topology { get; set; }

  public string? units { get; set; }

  [DetachProperty]
  public IReadOnlyList<Base> displayValue { get; set; }

  #region Schema Info Constructors
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
    string? name = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end2Offset = null,
    Plane? localAxis = null
  )
    : this(baseLine, property, type, name, null, end1Releases, end2Releases, end1Offset, end2Offset, localAxis) { }

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
    string? name = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end1Releases = null,
    [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")]
      Restraint? end2Releases = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end1Offset = null,
    [SchemaParamInfo("If null, defaults to no offsets")] Vector? end2Offset = null,
    Node? orientationNode = null,
    double orientationAngle = 0
  )
    : this(
      baseLine,
      property,
      type,
      name,
      null,
      end1Releases,
      end2Releases,
      end1Offset,
      end2Offset,
      orientationNode: orientationNode,
      orientationAngle: orientationAngle
    ) { }

  #endregion
}
