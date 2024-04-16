using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToSpeckleBaseList : IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>>
{
  private readonly IRawConversion<MapPoint, SOG.Point> _pointToSpeckleConverter;
  private readonly IRawConversion<Multipoint, IReadOnlyList<SOG.Point>> _multiPointFeatureConverter;
  private readonly IRawConversion<Polyline, IReadOnlyList<SOG.Polyline>> _polylineFeatureConverter;
  private readonly IRawConversion<Polygon, IReadOnlyList<GisPolygonGeometry>> _polygonFeatureConverter;
  private readonly IRawConversion<Multipatch, IReadOnlyList<GisPolygonGeometry>> _multipatchFeatureConverter;

  public GeometryToSpeckleBaseList(
    IRawConversion<MapPoint, SOG.Point> pointToSpeckleConverter,
    IRawConversion<Multipoint, IReadOnlyList<SOG.Point>> multiPointFeatureConverter,
    IRawConversion<Polyline, IReadOnlyList<SOG.Polyline>> polylineFeatureConverter,
    IRawConversion<Polygon, IReadOnlyList<GisPolygonGeometry>> polygonFeatureConverter,
    IRawConversion<Multipatch, IReadOnlyList<GisPolygonGeometry>> multipatchFeatureConverter
  )
  {
    _pointToSpeckleConverter = pointToSpeckleConverter;
    _multiPointFeatureConverter = multiPointFeatureConverter;
    _polylineFeatureConverter = polylineFeatureConverter;
    _polygonFeatureConverter = polygonFeatureConverter;
    _multipatchFeatureConverter = multipatchFeatureConverter;
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
        Multipatch multipatch => _multipatchFeatureConverter.RawConvert(multipatch),
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
