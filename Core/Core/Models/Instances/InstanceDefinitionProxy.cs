using System.Collections.Generic;

namespace Speckle.Core.Models.Instances;

/// <summary>
/// A proxy class for an instance definition.
/// </summary>
public class InstanceDefinitionProxy : Base, IInstanceComponent
{
  public List<string> Objects { get; set; } // source app application ids for the objects
  public int MaxDepth { get; set; }
}
