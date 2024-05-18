using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class SegmentCollectionToSpeckleConverter : IRawConversion<ACG.ReadOnlySegmentCollection, SOG.Polyline>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<ACG.LineSegment, List<SOG.Point>> _lineConverter;
  private readonly IRawConversion<ACG.EllipticArcSegment, List<SOG.Point>> _arcConverter;
  private readonly IRawConversion<ACG.CubicBezierSegment, List<SOG.Point>> _bezierConverter;

  public SegmentCollectionToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<ACG.LineSegment, List<SOG.Point>> lineConverter,
    IRawConversion<ACG.EllipticArcSegment, List<SOG.Point>> arcConverter,
    IRawConversion<ACG.CubicBezierSegment, List<SOG.Point>> bezierConverter
  )
  {
    _contextStack = contextStack;
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _bezierConverter = bezierConverter;
  }

  public SOG.Polyline RawConvert(ACG.ReadOnlySegmentCollection target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    double len = 0;

    List<SOG.Point> points = new();
    foreach (var segment in target)
    {
      len += segment.Length;

      // specific conversion per segment type
      switch (segment.SegmentType)
      {
        case ACG.SegmentType.Line:
          points = AddPtsToPolyline(points, _lineConverter.RawConvert((ACG.LineSegment)segment));
          break;
        case ACG.SegmentType.Bezier:
          points = AddPtsToPolyline(points, _bezierConverter.RawConvert((ACG.CubicBezierSegment)segment));
          break;
        case ACG.SegmentType.EllipticArc:
          points = AddPtsToPolyline(points, _arcConverter.RawConvert((ACG.EllipticArcSegment)segment));
          break;
      }
    }
    SOG.Polyline polyline =
      new(points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(), _contextStack.Current.SpeckleUnits) { };

    return polyline;
  }

  private List<SOG.Point> AddPtsToPolyline(List<SOG.Point> points, List<SOG.Point> newSegmentPts)
  {
    if (points.Count == 0 || points[^1] != newSegmentPts[0])
    {
      points.AddRange(newSegmentPts);
    }
    else
    {
      points.AddRange(newSegmentPts.GetRange(1, newSegmentPts.Count - 1));
    }
    return points;
  }
}
