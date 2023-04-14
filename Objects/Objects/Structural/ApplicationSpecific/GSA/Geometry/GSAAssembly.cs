using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Geometry;

public class GSAAssembly : Base
{
  public GSAAssembly() { }

  [SchemaInfo(
    "GSAAssembly",
    "Creates a Speckle structural assembly (ie. a way to define an entity that is formed from a collection of elements or members) for GSA",
    "GSA",
    "Bridge"
  )]
  public GSAAssembly(
    int nativeId,
    string name,
    List<Base> entities,
    GSANode end1Node,
    GSANode end2Node,
    GSANode orientationNode,
    double sizeY,
    double sizeZ,
    string curveType,
    int curveOrder,
    string pointDefinition,
    List<double> points
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.entities = entities;
    this.end1Node = end1Node;
    this.end2Node = end2Node;
    this.orientationNode = orientationNode;
    this.sizeY = sizeY;
    this.sizeZ = sizeZ;
    this.curveType = curveType;
    this.curveOrder = curveOrder;
    this.pointDefinition = pointDefinition;
    this.points = points;
  }

  public int nativeId { get; set; } //equiv to num record of gwa keyword
  public string name { get; set; }

  [DetachProperty, Chunkable(5000)]
  public List<Base> entities { get; set; } //nodes, elements, members

  [DetachProperty]
  public GSANode end1Node { get; set; }

  [DetachProperty]
  public GSANode end2Node { get; set; }

  [DetachProperty]
  public GSANode orientationNode { get; set; }

  public double sizeY { get; set; }
  public double sizeZ { get; set; }
  public string curveType { get; set; } // enum? circular or lagrange sufficient?
  public int curveOrder { get; set; }
  public string pointDefinition { get; set; } // enum as well? points and spacing to start? || points and storeys to be supported
  public List<double> points { get; set; } // or make this Base type to accomdate storey list and explicit range? or add sep property for those cases?
}
