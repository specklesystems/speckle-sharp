using Speckle.Core.Models;

namespace Objects.BuiltElements.TeklaStructures;

public class TeklaPosition : Base
{
  public TeklaDepthEnum Depth { get; set; }
  public TeklaPlaneEnum Plane { get; set; }
  public TeklaRotationEnum Rotation { get; set; }
  public double depthOffset { get; set; }
  public double planeOffset { get; set; }
  public double rotationOffset { get; set; }
}
