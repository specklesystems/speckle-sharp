using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Features;

public class MultipointFeatureToSpeckleConverter : IRawConversion<Multipoint, IReadOnlyList<SOG.Point>>
{
  private readonly IRawConversion<MapPoint, SOG.Point> _pointConverter;

  public MultipointFeatureToSpeckleConverter(IRawConversion<MapPoint, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public IReadOnlyList<SOG.Point> RawConvert(Multipoint target)
  {
    List<SOG.Point> multipoint = new();
    foreach (MapPoint point in target.Points)
    {
      multipoint.Add(_pointConverter.RawConvert(point));
    }

    return multipoint;
  }
}
