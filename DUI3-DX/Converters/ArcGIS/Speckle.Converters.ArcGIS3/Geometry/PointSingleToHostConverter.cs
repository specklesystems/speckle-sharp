using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PointToHostConverter : IRawConversion<SOG.Point, ACG.MapPoint>
{
  public object Convert(Base target) => RawConvert((SOG.Point)target);

  public ACG.MapPoint RawConvert(SOG.Point target)
  {
    return new ACG.MapPointBuilderEx(target.x, target.y, target.z).ToGeometry();
  }
}
