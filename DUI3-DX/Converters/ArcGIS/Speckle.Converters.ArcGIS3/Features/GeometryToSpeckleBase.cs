using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

public class GeometryToSpeckleBaseList : ITypedConverter<ACG.Geometry, IReadOnlyList<Base>>
{
  private readonly ITypedConverter<ACG.MapPoint, SOG.Point> _pointToSpeckleConverter;
  private readonly ITypedConverter<ACG.Multipoint, IReadOnlyList<SOG.Point>> _multiPointFeatureConverter;
  private readonly ITypedConverter<ACG.Polyline, IReadOnlyList<SOG.Polyline>> _polylineFeatureConverter;
  private readonly ITypedConverter<ACG.Polygon, IReadOnlyList<SGIS.PolygonGeometry>> _polygonFeatureConverter;
  private readonly ITypedConverter<ACG.Multipatch, IReadOnlyList<Base>> _multipatchFeatureConverter;

  public GeometryToSpeckleBaseList(
    ITypedConverter<ACG.MapPoint, SOG.Point> pointToSpeckleConverter,
    ITypedConverter<ACG.Multipoint, IReadOnlyList<SOG.Point>> multiPointFeatureConverter,
    ITypedConverter<ACG.Polyline, IReadOnlyList<SOG.Polyline>> polylineFeatureConverter,
    ITypedConverter<ACG.Polygon, IReadOnlyList<SGIS.PolygonGeometry>> polygonFeatureConverter,
    ITypedConverter<ACG.Multipatch, IReadOnlyList<Base>> multipatchFeatureConverter
  )
  {
    _pointToSpeckleConverter = pointToSpeckleConverter;
    _multiPointFeatureConverter = multiPointFeatureConverter;
    _polylineFeatureConverter = polylineFeatureConverter;
    _polygonFeatureConverter = polygonFeatureConverter;
    _multipatchFeatureConverter = multipatchFeatureConverter;
  }

  public IReadOnlyList<Base> RawConvert(ACG.Geometry target)
  {
    try
    {
      return target switch
      {
        ACG.MapPoint point => new List<SOG.Point>() { _pointToSpeckleConverter.RawConvert(point) },
        ACG.Multipoint multipoint => _multiPointFeatureConverter.RawConvert(multipoint),
        ACG.Polyline polyline => _polylineFeatureConverter.RawConvert(polyline),
        ACG.Polygon polygon => _polygonFeatureConverter.RawConvert(polygon),
        ACG.Multipatch multipatch => _multipatchFeatureConverter.RawConvert(multipatch), // GisMultipatchGeometry or PolygonGeometry3d
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
