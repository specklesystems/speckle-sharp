using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpecklePointRawToHostConversion
  : IRawConversion<SOG.Point, RG.Point3d>,
    IRawConversion<SOG.Point, RG.Point>
{
  public RG.Point3d RawConvert(SOG.Point target)
  {
    return new RG.Point3d(target.x, target.y, target.z);
  }

  RG.Point IRawConversion<SOG.Point, RG.Point>.RawConvert(SOG.Point target) => new(RawConvert(target));
}
