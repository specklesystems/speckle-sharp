using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class Polygon3dToHostConverter : IRawConversion<List<SGIS.GisPolygonGeometry3d>, ACG.Multipatch>
{
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public Polygon3dToHostConverter(IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public ACG.Multipatch RawConvert(List<SGIS.GisPolygonGeometry3d> target)
  {
    // TODO: implement multipatch receive
    List<ACG.Polygon> polyList = new();
    foreach (SGIS.GisPolygonGeometry poly in target)
    {
      // POC: add voids
      ACG.Polyline boundary = _polylineConverter.RawConvert(poly.boundary);
      polyList.Add(new ACG.PolygonBuilderEx(boundary).ToGeometry());
    }
    if (polyList.Count == 0)
    {
      throw new SpeckleConversionException("Feature contains no geometry");
    }
    throw new SpeckleConversionException("Feature contains no geometry");
    // return new ACG.PolygonBuilderEx(polyList, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}
