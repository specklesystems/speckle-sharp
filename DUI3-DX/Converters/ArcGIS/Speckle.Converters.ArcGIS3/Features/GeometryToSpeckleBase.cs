using ArcGIS.Core.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using MapPointFeature = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToSpeckleBaseList : IRawConversion<ArcGIS.Core.Geometry.Geometry, List<Base>>
{
  private readonly IRawConversion<MapPointFeature, List<Base>> _pointFeatureConverter;
  private readonly IRawConversion<Multipoint, List<Base>> _multiPointFeatureConverter;
  private readonly IRawConversion<Polyline, List<Base>> _polylineFeatureConverter;
  private readonly IRawConversion<Polygon, List<Base>> _polygonFeatureConverter;

  public GeometryToSpeckleBaseList(
    IRawConversion<MapPointFeature, List<Base>> pointFeatureConverter,
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
      Base newGeometry = new(); // objectConverter.Convert(target);
      if (target is MapPointFeature)
      {
        convertedList = _pointFeatureConverter.RawConvert((MapPointFeature)target);
        return convertedList;
      }
      if (target is Multipoint)
      {
        convertedList = _multiPointFeatureConverter.RawConvert((Multipoint)target);
        return convertedList;
      }
      if (target is Polyline)
      {
        convertedList = _polylineFeatureConverter.RawConvert((Polyline)target);
        return convertedList;
      }
      if (target is Polygon)
      {
        convertedList = _polygonFeatureConverter.RawConvert((Polygon)target);
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
