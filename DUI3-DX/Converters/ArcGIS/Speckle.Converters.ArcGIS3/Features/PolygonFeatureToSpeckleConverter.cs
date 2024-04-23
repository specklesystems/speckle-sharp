using Speckle.Converters.Common.Objects;
using Objects.GIS;

namespace Speckle.Converters.ArcGIS3.Features;

public class PolygonFeatureToSpeckleConverter : IRawConversion<ACG.Polygon, IReadOnlyList<GisPolygonGeometry>>
{
  private readonly IRawConversion<ACG.ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;

  public PolygonFeatureToSpeckleConverter(IRawConversion<ACG.ReadOnlySegmentCollection, SOG.Polyline> segmentConverter)
  {
    _segmentConverter = segmentConverter;
  }

  public IReadOnlyList<GisPolygonGeometry> RawConvert(ACG.Polygon target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic30235.html
    List<GisPolygonGeometry> polygonList = new();
    int partCount = target.PartCount;

    GisPolygonGeometry polygon = new() { };
    // test each part for "exterior ring"
    for (int idx = 0; idx < partCount; idx++)
    {
      ACG.ReadOnlySegmentCollection segmentCollection = target.Parts[idx];
      SOG.Polyline polyline = _segmentConverter.RawConvert(segmentCollection);

      bool isExteriorRing = target.IsExteriorRing(idx);
      if (isExteriorRing is true)
      {
        if (polygonList.Count > 0)
        {
          polygonList.Add(polygon);
        }
        polygon = new() { boundary = polyline, voids = new List<SOG.Polyline>() };
      }
      else
      {
        polygon.voids.Add(polyline);
      }
    }
    // TODO: if no voids, force boundary orientation to counter-clockwise
    polygonList.Add(polygon);

    return polygonList;
  }
}
