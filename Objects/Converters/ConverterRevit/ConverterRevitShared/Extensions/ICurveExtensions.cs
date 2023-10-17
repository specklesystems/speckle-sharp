using System.Collections.Generic;
using System.Linq;
using Objects;
using Objects.Geometry;

namespace ConverterRevitShared.Extensions
{
  internal static class ICurveExtensions
  {
    public static Point? StartPoint(this ICurve curve)
    {
      return curve switch
      {
        Line line => line.start,
        Arc arc => arc.startPoint,
        Spiral spiral => spiral.startPoint,
        Curve crv => new Point(crv.points[0], crv.points[1], crv.points[2], crv.units),
        Polyline poly => new Point(poly.value[0], poly.value[1], poly.value[2], poly.units),
        Polycurve plc => plc.segments.FirstOrDefault()?.StartPoint(),
        Ellipse ellipse => null,
        Circle circle => null,
        _ => null,
      };
    }
    public static Point? EndPoint(this ICurve curve)
    {
      return curve switch
      {
        Line line => line.end,
        Arc arc => arc.endPoint,
        Spiral spiral => spiral.endPoint,
        Curve crv => new Point(
          crv.points[crv.points.Count - 2], 
          crv.points[crv.points.Count - 1], 
          crv.points[crv.points.Count], 
          crv.units
        ),
        Polyline poly => new Point(
          poly.value[poly.value.Count - 2],
          poly.value[poly.value.Count - 1],
          poly.value[poly.value.Count],
          poly.units
        ),
        Polycurve plc => plc.segments.LastOrDefault()?.StartPoint(),
        Ellipse ellipse => null,
        Circle circle => null,
        _ => null,
      };
    }
    public static ICurve? GetReversed(this ICurve curve)
    {
      switch (curve)
      {
        case Line line:
          return new Line(line.end, line.start, line.units);
        case Arc arc:
          return new Arc(arc.plane, arc.endPoint, arc.startPoint, arc.angleRadians, arc.units);
        case Polyline poly:
          var points = poly.GetPoints();
          points.Reverse();
          IEnumerable<double> deconstructedPoints = points.SelectMany(p =>
          {
            return p.ToList();
          });
          return new Polyline(deconstructedPoints.ToList());
        case Polycurve plc:
          var segments = plc.segments;
          segments.Reverse();
          return new Polycurve()
          {
            segments = segments.Select(c => c.GetReversed()).ToList(),
          };
        default:
          return null;
      }
    }
  }
}
