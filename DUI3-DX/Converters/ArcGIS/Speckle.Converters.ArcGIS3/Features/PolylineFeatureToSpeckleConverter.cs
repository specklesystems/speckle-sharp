using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(ACG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolyineFeatureToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<ACG.Polyline, Base>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<ArcMapPoint, SOG.Point> _pointConverter;
  private readonly IRawConversion<ACG.ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;

  public PolyineFeatureToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<ArcMapPoint, SOG.Point> pointConverter,
    IRawConversion<ACG.ReadOnlySegmentCollection, SOG.Polyline> segmentConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
    _segmentConverter = segmentConverter;
  }

  public Base Convert(object target) => RawConvert((ACG.Polyline)target);

  public Base RawConvert(ACG.Polyline target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    List<Base> polylineList = new();
    foreach (var segmentCollection in target.Parts)
    {
      polylineList.Add(_segmentConverter.RawConvert(segmentCollection));
    }
    return polylineList[0];
  }
}
