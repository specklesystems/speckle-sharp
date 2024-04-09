using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpecklePointToHostConverter : IRawConversion<SOG.Point, RG.Point3d>
{
  public SpecklePointToHostConverter() { }

  public RG.Point3d RawConvert(SOG.Point target)
  {
    // POC: NO unit conversion
    return new RG.Point3d(target.x, target.y, target.x);
  }
}
