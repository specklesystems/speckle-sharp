using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class PointFeatureToSpeckleConverter : IRawConversion<MapPoint, List<SOG.Point>>
{
  private readonly IRawConversion<MapPoint, SOG.Point> _pointConverter;

  public PointFeatureToSpeckleConverter(IRawConversion<MapPoint, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public List<SOG.Point> RawConvert(MapPoint target)
  {
    return new() { _pointConverter.RawConvert(target) };
  }
}
