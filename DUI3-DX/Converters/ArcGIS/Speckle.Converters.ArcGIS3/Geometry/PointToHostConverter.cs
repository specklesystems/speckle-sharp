using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PointToHostConverter : IRawConversion<SOG.Point, ACG.Multipoint>
{
  public ACG.Multipoint RawConvert(SOG.Point target)
  {
    ACG.MapPoint mapPoint = new ACG.MapPointBuilderEx(target.x, target.y, target.z).ToGeometry();
    return new ACG.MultipointBuilderEx(mapPoint).ToGeometry();
  }
}
