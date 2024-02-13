using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Analysis;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Geometry;

public class GSARigidConstraint : Base
{
  public GSARigidConstraint() { }

  [SchemaInfo(
    "GSARigidConstraint",
    "Creates a Speckle structural rigid restraint (a set of nodes constrained to move as a rigid body) for GSA",
    "GSA",
    "Geometry"
  )]
  public GSARigidConstraint(
    string name,
    int nativeId,
    Node primaryNode,
    List<Node> constrainedNodes,
    Base parentMember,
    List<GSAStage> stages,
    LinkageType type,
    Dictionary<AxisDirection6, List<AxisDirection6>> constraintCondition
  )
  {
    this.name = name;
    this.nativeId = nativeId;
    this.primaryNode = primaryNode;
    this.constrainedNodes = constrainedNodes;
    this.parentMember = parentMember;
    this.stages = stages;
    this.type = type;
    this.constraintCondition = constraintCondition;
  }

  public string name { get; set; }
  public int nativeId { get; set; }

  [DetachProperty]
  public Node primaryNode { get; set; }

  [DetachProperty, Chunkable(5000)]
  public List<Node> constrainedNodes { get; set; }

  [DetachProperty]
  public Base parentMember { get; set; }

  [DetachProperty]
  public List<GSAStage> stages { get; set; }

  public LinkageType type { get; set; }
  public Dictionary<AxisDirection6, List<AxisDirection6>> constraintCondition { get; set; }
}

public enum AxisDirection6
{
  NotSet = 0,
  X,
  Y,
  Z,
  XX,
  YY,
  ZZ
}

public enum LinkageType
{
  NotSet = 0,
  ALL,
  XY_PLANE,
  YZ_PLANE,
  ZX_PLANE,
  XY_PLATE,
  YZ_PLATE,
  ZX_PLATE,
  PIN,
  XY_PLANE_PIN,
  YZ_PLANE_PIN,
  ZX_PLANE_PIN,
  XY_PLATE_PIN,
  YZ_PLATE_PIN,
  ZX_PLATE_PIN,
  Custom
}
