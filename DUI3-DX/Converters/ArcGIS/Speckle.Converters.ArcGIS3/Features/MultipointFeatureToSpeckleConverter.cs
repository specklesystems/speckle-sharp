using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Features;

public class MultipointFeatureToSpeckleConverter : IRawConversion<ACG.Multipoint, IReadOnlyList<SOG.Point>>
{
  private readonly IRawConversion<ACG.MapPoint, SOG.Point> _pointConverter;

  public MultipointFeatureToSpeckleConverter(IRawConversion<ACG.MapPoint, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public IReadOnlyList<SOG.Point> RawConvert(ACG.Multipoint target)
  {
    List<SOG.Point> multipoint = new();
    foreach (ACG.MapPoint point in target.Points)
    {
      multipoint.Add(_pointConverter.RawConvert(point));
    }

    return multipoint;
  }
}
