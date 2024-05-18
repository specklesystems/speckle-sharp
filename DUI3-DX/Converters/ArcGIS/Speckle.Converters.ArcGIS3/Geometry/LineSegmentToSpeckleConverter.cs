using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class LineSegmentToSpeckleConverter : IRawConversion<ACG.LineSegment, List<SOG.Point>>
{
  private readonly IRawConversion<ACG.MapPoint, SOG.Point> _pointConverter;

  public LineSegmentToSpeckleConverter(IRawConversion<ACG.MapPoint, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public List<SOG.Point> RawConvert(ACG.LineSegment target)
  {
    List<SOG.Point> points = new();
    points.Add(_pointConverter.RawConvert(target.StartPoint));
    points.Add(_pointConverter.RawConvert(target.EndPoint));
    return points;
  }
}
