using ArcGIS.Core.Geometry;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(ArcGIS.Core.Geometry.Geometry), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class GeometryToSpeckleBaseList
  : IHostObjectToSpeckleConversion,
    IRawConversion<ArcGIS.Core.Geometry.Geometry, Base>
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;
  private readonly IRawConversion<MapPoint, Base> _pointFeatureConverter;
  private readonly IRawConversion<Multipoint, Base> _multiPointFeatureConverter;
  private readonly IRawConversion<Polyline, Base> _polylineFeatureConverter;
  private readonly IRawConversion<Polygon, Base> _polygonFeatureConverter;

  public GeometryToSpeckleBaseList(
    IFactory<string, IHostObjectToSpeckleConversion> toSpeckle,
    IRawConversion<MapPoint, Base> pointFeatureConverter,
    IRawConversion<Multipoint, Base> multiPointFeatureConverter,
    IRawConversion<Polyline, Base> polylineFeatureConverter,
    IRawConversion<Polygon, Base> polygonFeatureConverter
  )
  {
    _toSpeckle = toSpeckle;
    _pointFeatureConverter = pointFeatureConverter;
    _multiPointFeatureConverter = multiPointFeatureConverter;
    _polylineFeatureConverter = polylineFeatureConverter;
    _polygonFeatureConverter = polygonFeatureConverter;
  }

  public Base Convert(object target) => RawConvert((ArcGIS.Core.Geometry.Geometry)target);

  public Base RawConvert(ArcGIS.Core.Geometry.Geometry target)
  {
    List<Base> convertedList = new();

    Type type = target.GetType();
    try
    {
      Base newGeometry = new(); // objectConverter.Convert(target);
      if (target is MapPoint)
      {
        newGeometry = _pointFeatureConverter.RawConvert((MapPoint)target);
        convertedList.Add(newGeometry);
        return convertedList[0];
      }
      if (target is Multipoint)
      {
        newGeometry = _multiPointFeatureConverter.RawConvert((Multipoint)target);
        convertedList.Add(newGeometry);
        return convertedList[0];
      }
      if (target is Polyline)
      {
        newGeometry = _polylineFeatureConverter.RawConvert((Polyline)target);
        convertedList.Add(newGeometry);
        return convertedList[0];
      }
      if (target is Polygon)
      {
        newGeometry = _polygonFeatureConverter.RawConvert((Polygon)target);
        convertedList.Add(newGeometry);
        return convertedList[0];
      }
      throw new NotSupportedException($"No conversion found for {type.Name}");
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
