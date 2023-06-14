using System.Collections.Generic;
using Objects.Geometry;

namespace ConverterCSIShared.Extensions
{
  internal static class PolycurveExtensions
  {
    public static IEnumerable<Point> ToPoints(this Polycurve polycurve)
    {
      foreach (var seg in polycurve.segments)
      {
        foreach (var point in seg.ToPoints())
        {
          yield return point;
        }
      }
    }
  }
}
