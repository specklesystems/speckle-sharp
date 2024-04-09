using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleVectorToHostConverter : IRawConversion<SOG.Point, RG.Vector3d>
{
  public SpeckleVectorToHostConverter() { }

  public RG.Vector3d RawConvert(SOG.Point target)
  {
    // POC: NO unit conversion
    return new RG.Vector3d(target.x, target.y, target.x);
  }
}
