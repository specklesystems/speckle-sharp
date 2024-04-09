using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class SegmentCollectionToSpeckleConverter : IRawConversion<ReadOnlySegmentCollection, SOG.Polyline>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<MapPoint, SOG.Point> _pointConverter;
  private readonly IRawConversion<EllipticArcSegment, SOG.Polyline> _arcConverter;
  private readonly IRawConversion<CubicBezierSegment, SOG.Polyline> _bezierConverter;

  public SegmentCollectionToSpeckleConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<MapPoint, SOG.Point> pointConverter,
    IRawConversion<EllipticArcSegment, SOG.Polyline> arcConverter,
    IRawConversion<CubicBezierSegment, SOG.Polyline> bezierConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
    _arcConverter = arcConverter;
    _bezierConverter = bezierConverter;
  }

  public Base Convert(object target) => RawConvert((ReadOnlySegmentCollection)target);

  public SOG.Polyline RawConvert(ReadOnlySegmentCollection target)
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
        case SegmentType.Line:
          points.Add(_pointConverter.RawConvert(segment.StartPoint));
          points.Add(_pointConverter.RawConvert(segment.EndPoint));
          break;
        case SegmentType.Bezier:
          var segmentBezier = (CubicBezierSegment)segment;
          points.AddRange(_bezierConverter.RawConvert(segmentBezier).GetPoints());
          break;
        case SegmentType.EllipticArc:
          var segmentElliptic = (EllipticArcSegment)segment;
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
