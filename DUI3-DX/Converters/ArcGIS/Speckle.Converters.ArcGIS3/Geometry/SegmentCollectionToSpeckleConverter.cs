using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class SegmentCollectionToSpeckleConverter : IRawConversion<ACG.ReadOnlySegmentCollection, SOG.Polyline>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<ACG.LineSegment, List<SOG.Point>> _lineConverter;

  public SegmentCollectionToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<ACG.LineSegment, List<SOG.Point>> lineConverter
  )
  {
    _contextStack = contextStack;
    _lineConverter = lineConverter;
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
        default:
          throw new SpeckleConversionException($"Segment of type '{segment.SegmentType}' cannot be converted");
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
