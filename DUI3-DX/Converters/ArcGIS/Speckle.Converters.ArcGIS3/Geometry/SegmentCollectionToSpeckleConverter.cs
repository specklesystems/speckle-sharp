using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class SegmentCollectionToSpeckleConverter : ITypedConverter<ACG.ReadOnlySegmentCollection, SOG.Polyline>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly ITypedConverter<ACG.MapPoint, SOG.Point> _pointConverter;
  private readonly ITypedConverter<ACG.EllipticArcSegment, SOG.Polyline> _arcConverter;
  private readonly ITypedConverter<ACG.CubicBezierSegment, SOG.Polyline> _bezierConverter;

  public SegmentCollectionToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    ITypedConverter<ACG.MapPoint, SOG.Point> pointConverter,
    ITypedConverter<ACG.EllipticArcSegment, SOG.Polyline> arcConverter,
    ITypedConverter<ACG.CubicBezierSegment, SOG.Polyline> bezierConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
    _arcConverter = arcConverter;
    _bezierConverter = bezierConverter;
  }

  public SOG.Polyline Convert(ACG.ReadOnlySegmentCollection target)
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
          points.Add(_pointConverter.Convert(segment.StartPoint));
          points.Add(_pointConverter.Convert(segment.EndPoint));
          break;
        case ACG.SegmentType.Bezier:
          var segmentBezier = (ACG.CubicBezierSegment)segment;
          points.AddRange(_bezierConverter.Convert(segmentBezier).GetPoints());
          break;
        case ACG.SegmentType.EllipticArc:
          var segmentElliptic = (ACG.EllipticArcSegment)segment;
          points.AddRange(_arcConverter.Convert(segmentElliptic).GetPoints());
          break;
      }
    }
    // var box = _boxConverter.Convert(target.Extent);
    SOG.Polyline polyline =
      new(points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(), _contextStack.Current.SpeckleUnits) { };

    return polyline;
  }
}
