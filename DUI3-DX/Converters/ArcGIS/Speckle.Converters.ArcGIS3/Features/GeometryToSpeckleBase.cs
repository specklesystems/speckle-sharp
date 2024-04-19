using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(ACG.Geometry), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class GeometryToSpeckleBaseList
  : IHostObjectToSpeckleConversion,
    IRawConversion<ArcGIS.Core.Geometry.Geometry, Base>
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;
  private readonly IRawConversion<ACG.MapPoint, Base> _pointFeatureConverter;
  private readonly IRawConversion<ACG.Multipoint, Base> _multiPointFeatureConverter;
  private readonly IRawConversion<ACG.Polyline, Base> _polylineFeatureConverter;
  private readonly IRawConversion<ACG.Polygon, Base> _polygonFeatureConverter;

  public GeometryToSpeckleBaseList(
    IFactory<string, IHostObjectToSpeckleConversion> toSpeckle,
    IRawConversion<ACG.MapPoint, Base> pointFeatureConverter,
    IRawConversion<ACG.Multipoint, Base> multiPointFeatureConverter,
    IRawConversion<ACG.Polyline, Base> polylineFeatureConverter,
    IRawConversion<ACG.Polygon, Base> polygonFeatureConverter
  )
  {
    _toSpeckle = toSpeckle;
    _pointFeatureConverter = pointFeatureConverter;
    _multiPointFeatureConverter = multiPointFeatureConverter;
    _polylineFeatureConverter = polylineFeatureConverter;
    _polygonFeatureConverter = polygonFeatureConverter;
  }

  public Base Convert(object target) => RawConvert((ACG.Geometry)target);

  public Base RawConvert(ACG.Geometry target)
  {
    List<Base> convertedList = new();

    Type type = target.GetType();
    try
    {
      Base newGeometry = new(); // objectConverter.Convert(target);
      if (target is ACG.MapPoint point)
      {
        newGeometry = _pointFeatureConverter.RawConvert(point);
        convertedList.Add(newGeometry);
        return convertedList[0];
      }
      if (target is ACG.Multipoint multipoint)
      {
        newGeometry = _multiPointFeatureConverter.RawConvert(multipoint);
        convertedList.Add(newGeometry);
        return convertedList[0];
      }
      if (target is ACG.Polyline polyline)
      {
        newGeometry = _polylineFeatureConverter.RawConvert(polyline);
        convertedList.Add(newGeometry);
        return convertedList[0];
      }
      if (target is ACG.Polygon polygon)
      {
        newGeometry = _polygonFeatureConverter.RawConvert(polygon);
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
