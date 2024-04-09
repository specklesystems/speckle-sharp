using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class MultipointFeatureToSpeckleConverter : IRawConversion<Multipoint, List<Base>>
{
  private readonly IRawConversion<MapPoint, SOG.Point> _pointConverter;

  public MultipointFeatureToSpeckleConverter(IRawConversion<MapPoint, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public List<Base> RawConvert(Multipoint target)
  {
    List<Base> multipoint = new();
    foreach (MapPoint point in target.Points)
    {
      multipoint.Add(_pointConverter.RawConvert(point));
    }

    return multipoint;
  }
}
