using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToSpeckleBaseList : IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>>
{
  private readonly IRawConversion<MapPoint, SOG.Point> _pointToSpeckleConverter;
  private readonly IRawConversion<Multipoint, List<SOG.Point>> _multiPointFeatureConverter;
  private readonly IRawConversion<Polyline, List<SOG.Polyline>> _polylineFeatureConverter;
  private readonly IRawConversion<Polygon, List<GisPolygonGeometry>> _polygonFeatureConverter;

  public GeometryToSpeckleBaseList(
    IRawConversion<MapPoint, SOG.Point> pointToSpeckleConverter,
    IRawConversion<Multipoint, List<SOG.Point>> multiPointFeatureConverter,
    IRawConversion<Polyline, List<SOG.Polyline>> polylineFeatureConverter,
    IRawConversion<Polygon, List<GisPolygonGeometry>> polygonFeatureConverter
  )
  {
    _pointToSpeckleConverter = pointToSpeckleConverter;
    _multiPointFeatureConverter = multiPointFeatureConverter;
    _polylineFeatureConverter = polylineFeatureConverter;
    _polygonFeatureConverter = polygonFeatureConverter;
  }

  public IReadOnlyList<Base> RawConvert(ArcGIS.Core.Geometry.Geometry target)
  {
    try
    {
      return target switch
      {
        MapPoint point => new List<SOG.Point>() { _pointToSpeckleConverter.RawConvert(point) },
        Multipoint multipoint => _multiPointFeatureConverter.RawConvert(multipoint),
        Polyline polyline => _polylineFeatureConverter.RawConvert(polyline),
        Polygon polygon => _polygonFeatureConverter.RawConvert(polygon),
        _ => throw new NotSupportedException($"No conversion found for {target.GetType().Name}"),
      };
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
