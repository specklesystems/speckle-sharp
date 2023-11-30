using System.Collections.Generic;
using Objects.Geometry;

namespace ConverterCSIShared.Extensions;

internal static class CurveExtensions
{
  public static IEnumerable<Point> ToPoints(this Curve curve)
  {
    for (int i = 0; i < curve.points.Count; i += 3)
    {
      yield return new Point(curve.points[i], curve.points[i + 1], curve.points[i + 2], curve.units);
    }
  }
}
