using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Features;

public class PolyineFeatureToSpeckleConverter : IRawConversion<Polyline, List<SOG.Polyline>>
{
  private readonly IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;

  public PolyineFeatureToSpeckleConverter(IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> segmentConverter)
  {
    _segmentConverter = segmentConverter;
  }

  public List<SOG.Polyline> RawConvert(Polyline target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    List<SOG.Polyline> polylineList = new();
    foreach (var segmentCollection in target.Parts)
    {
      polylineList.Add(_segmentConverter.RawConvert(segmentCollection));
    }
    return polylineList;
  }
}
