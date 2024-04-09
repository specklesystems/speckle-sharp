using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;

namespace Speckle.Converters.ArcGIS3.Features;

public class PolygonFeatureToSpeckleConverter : IRawConversion<Polygon, List<Base>>
{
  private readonly IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;

  public PolygonFeatureToSpeckleConverter(IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> segmentConverter)
  {
    _segmentConverter = segmentConverter;
  }

  public List<Base> RawConvert(Polygon target)
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

    return polygonList;
  }
}
