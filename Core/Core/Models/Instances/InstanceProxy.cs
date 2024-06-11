using System.DoubleNumerics;

namespace Speckle.Core.Models.Instances;

public class InstanceProxy : Base, IInstanceComponent
{
  public string DefinitionId { get; set; }
  public Matrix4x4 Transform { get; set; }
  public int MaxDepth { get; set; }
}
