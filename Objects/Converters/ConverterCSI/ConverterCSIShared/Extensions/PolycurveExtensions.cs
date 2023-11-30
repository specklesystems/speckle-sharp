using System;
using System.Collections.Generic;
using Objects.Geometry;

namespace ConverterCSIShared.Extensions;

internal static class PolycurveExtensions
{
  public static IEnumerable<Point> ToPoints(this Polycurve polycurve)
  {
    var prevPoint = new Point(double.NaN, double.NaN, double.NaN);
    foreach (var seg in polycurve.segments)
    {
      foreach (var point in seg.ToPoints())
      {
        if (
          Math.Abs(point.x - prevPoint.x) < .01
          && Math.Abs(point.y - prevPoint.y) < .01
          && Math.Abs(point.z - prevPoint.z) < .01
        )
        {
          continue;
        }
        prevPoint = point;
        yield return point;
      }
    }
  }
}
