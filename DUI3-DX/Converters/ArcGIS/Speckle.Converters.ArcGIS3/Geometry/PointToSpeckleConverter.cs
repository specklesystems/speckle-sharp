using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Geometry;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(MapPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
internal class PointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<MapPoint, Point>
{
  public Base Convert(object target) => RawConvert((MapPoint)target);

  public Point RawConvert(MapPoint target) => new(target.X, target.Y, target.Z, "m");
}
