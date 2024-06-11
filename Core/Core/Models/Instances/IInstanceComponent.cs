namespace Speckle.Core.Models.Instances;

/// <summary>
/// Abstracts over <see cref="InstanceProxy"/> and <see cref="InstanceDefinitionProxy"/> for sorting and grouping in receive operations.
/// </summary>
public interface IInstanceComponent
{
  /// <summary>
  /// The maximum nesting depth at which this component (Instance or Instance Definition) was found.
  /// </summary>
  public int MaxDepth { get; set; }
}
