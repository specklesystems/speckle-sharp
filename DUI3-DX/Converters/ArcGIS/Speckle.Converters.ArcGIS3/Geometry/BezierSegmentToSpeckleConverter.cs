using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class BezierSegmentToSpeckleConverter : IRawConversion<ACG.CubicBezierSegment, List<SOG.Point>>
{
  private readonly IRawConversion<ACG.MapPoint, SOG.Point> _pointConverter;

  public BezierSegmentToSpeckleConverter(IRawConversion<ACG.MapPoint, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public List<SOG.Point> RawConvert(ACG.CubicBezierSegment target)
  {
    // Determine the number of vertices to create along the arc
    int numVertices = Math.Max((int)target.Length, 3); // Determine based on desired segment length or other criteria
    List<SOG.Point> points = new();

    // Calculate the vertices along the curve
    for (int i = 0; i <= numVertices; i++)
    {
      double t = i / (double)numVertices;

      // Calculate the point using the cubic Bezier formula
      double x =
        (1 - t) * (1 - t) * (1 - t) * target.StartPoint.X
        + 3 * (1 - t) * (1 - t) * t * target.ControlPoint1.X
        + 3 * (1 - t) * t * t * target.ControlPoint2.X
        + t * t * t * target.EndPoint.X;

      double y =
        (1 - t) * (1 - t) * (1 - t) * target.StartPoint.Y
        + 3 * (1 - t) * (1 - t) * t * target.ControlPoint1.Y
        + 3 * (1 - t) * t * t * target.ControlPoint2.Y
        + t * t * t * target.EndPoint.Y;

      ACG.MapPoint pointOnCurve = ACG.MapPointBuilderEx.CreateMapPoint(x, y, target.SpatialReference);
      points.Add(_pointConverter.RawConvert(pointOnCurve));
    }

    return points;
  }
}
