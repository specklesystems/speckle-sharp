using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Loading;

public class LoadGravity : Load
{
  public LoadGravity() { }

  /// <summary>
  /// A gravity load (applied to all elements)
  /// </summary>
  /// <param name="name">A name or description to identify the load</param>
  /// <param name="loadCase">The load case in which the load applies</param>
  /// <param name="gravityFactors">A list of factors that apply on the “magnitude" of gravity (in terms of g, accleration of gravity) in each of the global axis (x, y and z) directions. Ex. For a model with global z-axis vertically upwards, the gravity factors of (0, 0, −1) represent a normal vertical gravity load on the structure</param>
  [SchemaInfo(
    "Gravity Load (all elements)",
    "Creates a Speckle structural gravity load (applied to all nodes and elements)",
    "Structural",
    "Loading"
  )]
  public LoadGravity(LoadCase loadCase, Vector gravityFactors = null, string name = null)
  {
    this.loadCase = loadCase;
    this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
    this.name = name;
  }

  /// <summary>
  /// A gravity load (applied to the specified elements)
  /// </summary>
  /// <param name="name">A name or description to identify the load</param>
  /// <param name="loadCase">The load case in which the load applies</param>
  /// <param name="elements">A list of elements to apply the load to</param>
  /// <param name="gravityFactors">A list of factors that apply on the “magnitude" of gravity (in terms of g, accleration of gravity) in each of the global axis (x, y and z) directions. Ex. For a model with global z-axis vertically upwards, the gravity factors of (0, 0, −1) represent a normal vertical gravity load on the structure</param>
  [SchemaInfo(
    "Gravity Load (specified elements)",
    "Creates a Speckle structural gravity load (applied to specified elements)",
    "Structural",
    "Loading"
  )]
  public LoadGravity(LoadCase loadCase, List<Base> elements, Vector gravityFactors = null, string name = null)
  {
    this.elements = elements;
    this.loadCase = loadCase;
    this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
    this.name = name;
  }

  /// <summary>
  /// A gravity load (applied to the specified elements and nodes)
  /// </summary>
  /// <param name="name">A name or description to identify the load</param>
  /// <param name="loadCase">The load case in which the load applies</param>
  /// <param name="elements">A list of elements to apply the load to</param>
  /// <param name="nodes">A list of nodes to apply the load to</param>
  /// <param name="gravityFactors">A list of factors that apply on the “magnitude" of gravity (in terms of g, accleration of gravity) in each of the global axis (x, y and z) directions. Ex. For a model with global z-axis vertically upwards, the gravity factors of (0, 0, −1) represent a normal vertical gravity load on the structure</param>
  [SchemaInfo(
    "Gravity Load (specified elements and nodes)",
    "Creates a Speckle structural gravity load (applied to specified nodes and elements)",
    "Structural",
    "Loading"
  )]
  public LoadGravity(
    LoadCase loadCase,
    List<Base> elements,
    List<Base> nodes,
    Vector gravityFactors = null,
    string name = null
  )
  {
    this.elements = elements;
    this.nodes = nodes;
    this.loadCase = loadCase;
    this.gravityFactors = gravityFactors == null ? new Vector(0, 0, -1) : gravityFactors;
    this.name = name;
  }

  [DetachProperty, Chunkable(5000)]
  public List<Base> elements { get; set; }

  [DetachProperty, Chunkable(5000)]
  public List<Base> nodes { get; set; }

  public Vector gravityFactors { get; set; } // a normal vertical gravity load is Z = -1
}
