using System.DoubleNumerics;

namespace Speckle.Core.Models.Instances;

/// <summary>
/// A proxy class for an instance (e.g, a rhino block, or an autocad block reference).
/// </summary>
public class InstanceProxy : Base, IInstanceComponent
{
  /// <summary>
  /// The definition id as present in the original host app. On receive, it will be mapped to the newly created definition id.
  /// </summary>
  public string DefinitionId { get; set; }

  /// <summary>
  /// The transform of the instance reference.
  /// </summary>
  public Matrix4x4 Transform { get; set; }

  /// <summary>
  /// The units of the host application file.
  /// </summary>
  public string Units { get; set; } = Kits.Units.Meters;

  /// <summary>
  /// The maximum "depth" at which this instance was found. It's important to get right: as instances can be composed of other instances, we need to start from the deepest instance elements first when reconstructing them.
  /// </summary>
  public int MaxDepth { get; set; }
}
