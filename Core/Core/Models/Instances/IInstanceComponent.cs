namespace Speckle.Core.Models.Instances;

/// <summary>
/// Abstracts over <see cref="InstanceProxy"/> and <see cref="InstanceDefinitionProxy"/> for sorting and grouping in receive operations.
/// </summary>
public interface IInstanceComponent
{
  /// <summary>
  /// The maximum "depth" at which this <see cref="InstanceProxy"/> or <see cref="InstanceDefinitionProxy"/> was found. On receive, as instances can be composed of other instances, we need to start from the deepest instance elements first when reconstructing them, starting with definitions first.
  /// </summary>
  public int MaxDepth { get; set; }
}
