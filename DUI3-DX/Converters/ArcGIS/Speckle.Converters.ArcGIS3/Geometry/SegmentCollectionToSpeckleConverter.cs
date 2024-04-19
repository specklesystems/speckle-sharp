using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(ACG.ReadOnlySegmentCollection), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SegmentCollectionToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<ACG.ReadOnlySegmentCollection, SOG.Polyline>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<ArcMapPoint, SOG.Point> _pointConverter;
  private readonly IRawConversion<ACG.EllipticArcSegment, SOG.Polyline> _arcConverter;
  private readonly IRawConversion<ACG.CubicBezierSegment, SOG.Polyline> _bezierConverter;

  public SegmentCollectionToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<ArcMapPoint, SOG.Point> pointConverter,
    IRawConversion<ACG.EllipticArcSegment, SOG.Polyline> arcConverter,
    IRawConversion<ACG.CubicBezierSegment, SOG.Polyline> bezierConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
    _arcConverter = arcConverter;
    _bezierConverter = bezierConverter;
  }

  public Base Convert(object target) => RawConvert((ACG.ReadOnlySegmentCollection)target);

  public SOG.Polyline RawConvert(ACG.ReadOnlySegmentCollection target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    double len = 0;

    List<SOG.Point> points = new();
    foreach (var segment in target)
    {
      len += segment.Length;

      // do something specific per segment type
      switch (segment.SegmentType)
      {
        case ACG.SegmentType.Line:
          points.Add(_pointConverter.RawConvert(segment.StartPoint));
          points.Add(_pointConverter.RawConvert(segment.EndPoint));
          break;
        case ACG.SegmentType.Bezier:
          var segmentBezier = (ACG.CubicBezierSegment)segment;
          points.AddRange(_bezierConverter.RawConvert(segmentBezier).GetPoints());
          break;
        case ACG.SegmentType.EllipticArc:
          var segmentElliptic = (ACG.EllipticArcSegment)segment;
          points.AddRange(_arcConverter.RawConvert(segmentElliptic).GetPoints());
          break;
      }
    }
    // var box = _boxConverter.RawConvert(target.Extent);
    SOG.Polyline polyline =
      new(points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(), _contextStack.Current.SpeckleUnits) { };

    return polyline;
  }
}
