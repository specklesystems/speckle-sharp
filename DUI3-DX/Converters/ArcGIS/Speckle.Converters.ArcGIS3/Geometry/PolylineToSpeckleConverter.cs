using Polyline = ArcGIS.Core.Geometry.Polyline;
using Unit = ArcGIS.Core.Geometry.Unit;
using MapPoint = ArcGIS.Core.Geometry.MapPoint;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolyineToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<Polyline, Objects.Geometry.Polyline>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<MapPoint, Objects.Geometry.Point> _pointConverter;

  public PolyineToSpeckleConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<MapPoint, Objects.Geometry.Point> pointConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((Polyline)target);

  public Objects.Geometry.Polyline RawConvert(Polyline target)
  {
    List<Objects.Geometry.Point> points = new();
    foreach (MapPoint pt in target.Points)
    {
      points.Add(_pointConverter.RawConvert(pt));
    }

    // var box = _boxConverter.RawConvert(target.Extent);

    return new Objects.Geometry.Polyline(
      points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(),
      _contextStack.Current.SpeckleUnits
    )
    {
      // bbox = box,
      length = target.Length
    };
  }
}
