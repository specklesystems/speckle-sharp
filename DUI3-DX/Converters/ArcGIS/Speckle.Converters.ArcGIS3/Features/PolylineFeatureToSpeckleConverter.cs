using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Features;

public class PolyineFeatureToSpeckleConverter : ITypedConverter<ACG.Polyline, IReadOnlyList<SOG.Polyline>>
{
  private readonly ITypedConverter<ACG.ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;

  public PolyineFeatureToSpeckleConverter(ITypedConverter<ACG.ReadOnlySegmentCollection, SOG.Polyline> segmentConverter)
  {
    _segmentConverter = segmentConverter;
  }

  public IReadOnlyList<SOG.Polyline> Convert(ACG.Polyline target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    List<SOG.Polyline> polylineList = new();
    foreach (var segmentCollection in target.Parts)
    {
      polylineList.Add(_segmentConverter.Convert(segmentCollection));
    }
    return polylineList;
  }
}
