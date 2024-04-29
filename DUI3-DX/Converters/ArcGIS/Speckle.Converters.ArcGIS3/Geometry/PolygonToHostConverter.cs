using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PolygonToHostConverter : IRawConversion<List<SGIS.GisPolygonGeometry>, ACG.Polygon>
{
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public PolygonToHostConverter(IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public ACG.Polygon RawConvert(List<SGIS.GisPolygonGeometry> target)
  {
    if (target.Count == 0)
    {
      throw new SpeckleConversionException("Feature contains no geometry");
    }

    List<ACG.Polygon> polyList = new();
    foreach (SGIS.GisPolygonGeometry poly in target)
    {
      ACG.Polyline boundary = _polylineConverter.RawConvert(poly.boundary);
      ACG.PolygonBuilderEx polyOuterRing = new(boundary);

      foreach (SOG.Polyline loop in poly.voids)
      {
        // adding inner loops: https://github.com/esri/arcgis-pro-sdk/wiki/ProSnippets-Geometry#build-a-donut-polygon
        ACG.Polyline loop_native = _polylineConverter.RawConvert(loop);
        polyOuterRing.AddPart(loop_native.Copy3DCoordinatesToList());
      }
      ACG.Polygon polygon = polyOuterRing.ToGeometry();
      polyList.Add(polygon);
    }
    return new ACG.PolygonBuilderEx(polyList, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}
