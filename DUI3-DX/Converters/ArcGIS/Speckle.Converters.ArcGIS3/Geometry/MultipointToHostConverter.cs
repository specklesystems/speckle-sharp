using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PointToHostConverter : IRawConversion<SOG.Point, ACG.MapPoint>
{
  public ACG.MapPoint RawConvert(SOG.Point target)
  {
    return new ACG.MapPointBuilderEx(target.x, target.y, target.z).ToGeometry();
  }
}
