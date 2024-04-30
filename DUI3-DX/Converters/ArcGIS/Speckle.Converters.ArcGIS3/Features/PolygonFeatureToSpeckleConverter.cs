using Speckle.Converters.Common.Objects;
using Objects.GIS;
using Speckle.Converters.Common;

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

    if (partCount == 0)
    {
      throw new SpeckleConversionException("ArcGIS Polygon contains no parts");
    }

    GisPolygonGeometry? polygon = null;

    // test each part for "exterior ring"
    for (int idx = 0; idx < partCount; idx++)
    {
      ACG.ReadOnlySegmentCollection segmentCollection = target.Parts[idx];
      SOG.Polyline polyline = _segmentConverter.RawConvert(segmentCollection);

      bool isExteriorRing = target.IsExteriorRing(idx);
      if (isExteriorRing is true)
      {
        polygon = new() { boundary = polyline, voids = new List<SOG.Polyline>() };
        polygonList.Add(polygon);
      }
      else // interior part
      {
        if (polygon == null)
        {
          throw new SpeckleConversionException("Invalid ArcGIS Polygon. Interior part preceeding the exterior ring.");
        }
        polygon.voids.Add(polyline);
      }
    }

    return polygonList;
  }
}
