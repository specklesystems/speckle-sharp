using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class PolyineFeatureToSpeckleConverter : IRawConversion<Polyline, List<Base>>
{
  private readonly IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;

  public PolyineFeatureToSpeckleConverter(IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> segmentConverter)
  {
    _segmentConverter = segmentConverter;
  }

  public List<Base> RawConvert(Polyline target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    List<Base> polylineList = new();
    foreach (var segmentCollection in target.Parts)
    {
      polylineList.Add(_segmentConverter.RawConvert(segmentCollection));
    }
    return polylineList;
  }
}
