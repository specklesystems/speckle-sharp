namespace Speckle.Core.Models.Instances;

public interface IInstanceComponent
{
  /// <summary>
  /// The maximum nesting depth at which this component (Instance or Instance Definition) was found.
  /// </summary>
  public int MaxDepth { get; set; }
}
