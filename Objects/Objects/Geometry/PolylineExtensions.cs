using System.Collections.Generic;

namespace Objects.Geometry;

public static class PolylineExtensions
{
  public static IEnumerable<Line> EnumerateAsLines(this Polyline polyline)
  {
    List<Point> points = polyline.GetPoints();
    if (points.Count == 0)
    {
      yield break;
    }

    Point previousPoint = points[0];
    for (int i = 1; i < points.Count; i++)
    {
      yield return new Line(previousPoint, points[i], polyline.units);
      previousPoint = points[i];
    }
  }
}
