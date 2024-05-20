using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Features;

public class MultipointFeatureToSpeckleConverter : ITypedConverter<ACG.Multipoint, IReadOnlyList<SOG.Point>>
{
  private readonly ITypedConverter<ACG.MapPoint, SOG.Point> _pointConverter;

  public MultipointFeatureToSpeckleConverter(ITypedConverter<ACG.MapPoint, SOG.Point> pointConverter)
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
