using ArcGIS.Core.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToSpeckleBaseList : IRawConversion<ArcGIS.Core.Geometry.Geometry, List<Base>>
{
  private readonly IRawConversion<MapPoint, List<Base>> _pointFeatureConverter;
  private readonly IRawConversion<Multipoint, List<Base>> _multiPointFeatureConverter;
  private readonly IRawConversion<Polyline, List<Base>> _polylineFeatureConverter;
  private readonly IRawConversion<Polygon, List<Base>> _polygonFeatureConverter;

  public GeometryToSpeckleBaseList(
    IRawConversion<MapPoint, List<Base>> pointFeatureConverter,
    IRawConversion<Multipoint, List<Base>> multiPointFeatureConverter,
    IRawConversion<Polyline, List<Base>> polylineFeatureConverter,
    IRawConversion<Polygon, List<Base>> polygonFeatureConverter
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
