using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToHostConverter : IRawConversion<Base, ACG.Geometry>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;
  private readonly IRawConversion<SOG.Point, ACG.Multipoint> _pointConverter;
  private readonly IRawConversion<GisPolygonGeometry, ACG.Polygon> _polygonConverter;
  private readonly IRawConversion<SOG.Mesh, ACG.Multipatch> _multipatchConverter;

  public GeometryToHostConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter,
    IRawConversion<SOG.Point, ACG.Multipoint> pointConverter,
    IRawConversion<GisPolygonGeometry, ACG.Polygon> polygonConverter,
    IRawConversion<SOG.Mesh, ACG.Multipatch> multipatchConverter
  )
  {
    _contextStack = contextStack;
    _polylineConverter = polylineConverter;
    _pointConverter = pointConverter;
    _polygonConverter = polygonConverter;
    _multipatchConverter = multipatchConverter;
  }

  public ACG.Geometry RawConvert(Base target)
  {
    try
    {
      return target switch
      {
        SOG.Point point => _pointConverter.RawConvert(point),
        SOG.Polyline polyline => _polylineConverter.RawConvert(polyline),
        GisPolygonGeometry geometry => _polygonConverter.RawConvert(geometry),
        SOG.Mesh mesh => _multipatchConverter.RawConvert(mesh),
        _ => throw new NotSupportedException($"No conversion found for {target.speckle_type}"),
      };
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // log errors
    }
  }
}
