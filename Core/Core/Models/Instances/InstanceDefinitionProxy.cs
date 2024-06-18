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

  public int MaxDepth { get; set; }
}
