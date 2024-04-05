using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;
using Objects.GIS;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(Polygon), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolygonFeatureToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Polygon, Base>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<ArcMapPoint, SOG.Point> _pointConverter;
  private readonly IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;

  public PolygonFeatureToSpeckleConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<ArcMapPoint, SOG.Point> pointConverter,
    IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> segmentConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
    _segmentConverter = segmentConverter;
  }

  public Base Convert(object target) => RawConvert((Polygon)target);

  public Base RawConvert(Polygon target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic30235.html
    List<Base> polygonList = new();
    int partCount = target.PartCount;

    GisPolygonGeometry polygon = new() { voids = new List<SOG.Polyline>() };
    // test each part for "exterior ring"
    for (int idx = 0; idx < partCount; idx++)
    {
      ReadOnlySegmentCollection segmentCollection = target.Parts[idx];
      SOG.Polyline polyline = _segmentConverter.RawConvert(segmentCollection);

      bool isExteriorRing = target.IsExteriorRing(idx);
      if (isExteriorRing is true)
      {
        if (polygonList.Count > 0)
        {
          polygonList.Add(polygon);
        }
        polygon = new() { voids = new List<SOG.Polyline>(), boundary = polyline };
      }
      else
      {
        polygon.voids.Add(polyline);
      }
    }
    polygonList.Add(polygon);

    return polygonList[0];
  }
}
