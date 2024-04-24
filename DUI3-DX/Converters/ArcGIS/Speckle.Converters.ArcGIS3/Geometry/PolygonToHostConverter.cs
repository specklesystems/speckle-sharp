using Speckle.Converters.Common.Objects;
using Objects.GIS;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PolygonToHostConverter : IRawConversion<GisPolygonGeometry, ACG.Polygon>
{
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public PolygonToHostConverter(IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public ACG.Polygon RawConvert(GisPolygonGeometry target)
  {
    // POC: add voids
    var boundary = _polylineConverter.RawConvert(target.boundary);
    ACG.PolygonBuilderEx polygonBuilder = new(boundary);
    ACG.Polygon polygon = polygonBuilder.ToGeometry();

    return polygon;
  }
}
