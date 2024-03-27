using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Geometry;

namespace Speckle.Converters.ArcGIS3.Geometry;

internal class PointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<MapPoint, Point>
{
  public Base Convert(object target) => RawConvert((MapPoint)target);

  public Point RawConvert(MapPoint target) => new(target.X, target.Y, target.Z, "m");
}
