using System.Collections.Generic;
using Objects.Structural.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Loading;

public class LoadNode : Load
{
  public LoadNode() { }

  /// <summary>
  /// A node load (applied in the global axis)
  /// </summary>
  /// <param name="loadCase">The load case in which the load applies</param>
  /// <param name="nodes">>A list of nodes to apply the load to</param>
  /// <param name="direction">The direction of the loading, relative to the specified axis</param>
  /// <param name="value">The magnitude of the load, either a force or moment</param>
  /// <param name="name">A name or description to identify the load</param>///
  [SchemaInfo("Node Load", "Creates a Speckle node load", "Structural", "Loading")]
  public LoadNode(LoadCase loadCase, List<Node> nodes, LoadDirection direction, double value, string? name = null)
  {
    this.name = name;
    this.loadCase = loadCase;
    this.nodes = nodes;
    this.direction = direction;
    this.value = value;
  }

  /// <summary>
  /// A node load (based on a user-defined axis)
  /// </summary>
  /// <param name="loadCase">The load case in which the load applies</param>
  /// <param name="nodes">>A list of nodes to apply the load to</param>
  /// <param name="loadAxis">The axis in which the load is applied</param>
  /// <param name="direction">The direction of the loading, relative to the specified axis</param>
  /// <param name="value">The magnitude of the load, either a force or moment</param>
  /// <param name="name">A name or description to identify the load</param>///
  [SchemaInfo(
    "Node Load (user-defined axis)",
    "Creates a Speckle node load (specifed using a user-defined axis)",
    "Structural",
    "Loading"
  )]
  public LoadNode(
    LoadCase loadCase,
    List<Node> nodes,
    Axis loadAxis,
    LoadDirection direction,
    double value,
    string? name = null
  )
  {
    this.name = name;
    this.loadCase = loadCase;
    this.nodes = nodes;
    this.loadAxis = loadAxis;
    this.direction = direction;
    this.value = value;
  }

  [DetachProperty, Chunkable(5000)]
  public List<Node> nodes { get; set; }

  [DetachProperty]
  public Axis loadAxis { get; set; }

  public LoadDirection direction { get; set; }
  public double value { get; set; } //a force or a moment, displacement (translation or rotation) and settlement to be covered in other classes
}
