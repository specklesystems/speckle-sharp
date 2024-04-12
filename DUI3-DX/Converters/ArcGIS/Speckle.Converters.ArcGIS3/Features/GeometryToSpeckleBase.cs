using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToSpeckleBaseList : IRawConversion<ArcGIS.Core.Geometry.Geometry, List<Base>>
{
  private readonly IRawConversion<MapPoint, List<SOG.Point>> _pointFeatureConverter;
  private readonly IRawConversion<Multipoint, List<SOG.Point>> _multiPointFeatureConverter;
  private readonly IRawConversion<Polyline, List<SOG.Polyline>> _polylineFeatureConverter;
  private readonly IRawConversion<Polygon, List<GisPolygonGeometry>> _polygonFeatureConverter;

  public GeometryToSpeckleBaseList(
    IRawConversion<MapPoint, List<SOG.Point>> pointFeatureConverter,
    IRawConversion<Multipoint, List<SOG.Point>> multiPointFeatureConverter,
    IRawConversion<Polyline, List<SOG.Polyline>> polylineFeatureConverter,
    IRawConversion<Polygon, List<GisPolygonGeometry>> polygonFeatureConverter
  )
  {
    _pointFeatureConverter = pointFeatureConverter;
    _multiPointFeatureConverter = multiPointFeatureConverter;
    _polylineFeatureConverter = polylineFeatureConverter;
    _polygonFeatureConverter = polygonFeatureConverter;
  }

  public List<Base> RawConvert(ArcGIS.Core.Geometry.Geometry target)
  {
    List<Base> convertedList;

    try
    {
      if (target is MapPoint point)
      {
        convertedList = _pointFeatureConverter.RawConvert(point);
        return convertedList;
      }
      if (target is Multipoint multipoint)
      {
        convertedList = _multiPointFeatureConverter.RawConvert(multipoint);
        return convertedList;
      }
      if (target is Polyline polyline)
      {
        convertedList = _polylineFeatureConverter.RawConvert(polyline);
        return convertedList;
      }
      if (target is Polygon polygon)
      {
        convertedList = _polygonFeatureConverter.RawConvert(polygon);
        return convertedList;
      }

      Type type = target.GetType();
      throw new NotSupportedException($"No conversion found for {type.Name}");
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
