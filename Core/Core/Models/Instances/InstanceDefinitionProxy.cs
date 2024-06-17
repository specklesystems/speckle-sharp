using System.Collections.Generic;

namespace Speckle.Core.Models.Instances;

/// <summary>
/// A proxy class for an instance definition.
/// </summary>
public class InstanceDefinitionProxy : Base, IInstanceComponent
{
  /// <summary>
  /// The original ids of the objects that are part of this definition, as present in the source host app. On receive, they will be mapped to corresponding newly created definition ids.
  /// </summary>
  public List<string> Objects { get; set; } // source app application ids for the objects

  /// <summary>
  /// The maximum "depth" at which this instance was found. It's important to get right: as instances can be composed of other instances, we need to start from the deepest instance elements first when reconstructing them.
  /// </summary>
  public int MaxDepth { get; set; }
}
