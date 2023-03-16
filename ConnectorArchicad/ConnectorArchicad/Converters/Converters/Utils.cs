using System.Collections.Generic;
using System.Linq;
using Archicad.Model;
using Objects;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Archicad.Converters
{
  public static class Utils
  {
    public static Point VertexToPoint (MeshModel.Vertex vertex)
    {
      return new Point { x = vertex.x, y = vertex.y , z = vertex.z };
    }

    public static Point ScaleToNative(Point point, string? units = null)
    {
      units ??= point.units;
      var scale = Units.GetConversionFactor(units, Units.Meters);

      return new Point(point.x * scale, point.y * scale, point.z * scale);
    }

    public static double ScaleToNative(double value, string sourceUnits)
    {
      return value * Units.GetConversionFactor(sourceUnits, Units.Meters);
    }

    public static MeshModel.Vertex PointToNative(Point point, string? units = null)
    {
      units ??= point.units;
      var scale = Units.GetConversionFactor(units, Units.Meters);

      return new MeshModel.Vertex { x = point.x * scale, y = point.y * scale, z = point.z * scale };
    }

    public static Polycurve PolycurveToSpeckle(ElementShape.Polyline archiPolyline)
    {
      var poly = new Polycurve
      {
        units = Units.Meters,
        closed = archiPolyline.polylineSegments.First().startPoint == archiPolyline.polylineSegments.Last().endPoint
      };
      foreach (var segment in archiPolyline.polylineSegments)
      {
        poly.segments.Add(segment.arcAngle == 0
          ? new Line(segment.startPoint, segment.endPoint)
          : new Arc(segment.startPoint, segment.endPoint, segment.arcAngle));
      }

      return poly;
    }

    public static ElementShape.PolylineSegment LineToNative(Line line)
    {
      return new ElementShape.PolylineSegment(ScaleToNative(line.start), ScaleToNative(line.end));
    }

    public static ElementShape.Polyline PolycurveToNative(Polycurve polycurve)
    {
      var segments = polycurve.segments.Select(CurveSegmentToNative).ToList();
      return new ElementShape.Polyline(segments);
    }

    public static ElementShape.Polyline PolylineToNative(Polyline polyline)
    {
      var archiPoly = new ElementShape.Polyline();
      var points = polyline.GetPoints();
      points.ForEach(p => ScaleToNative(p));
      for (var i = 0; i < points.Count - 1; i++)
      {
        archiPoly.polylineSegments.Add(new ElementShape.PolylineSegment(points[i], points[i + 1]));
      }

      return archiPoly;
    }

    public static ElementShape.PolylineSegment ArcToNative(Arc arc)
    {
      return new ElementShape.PolylineSegment(ScaleToNative(arc.startPoint), ScaleToNative(arc.endPoint),
        arc.angleRadians);
    }

    public static ElementShape.Polyline? CurveToNative(ICurve curve)
    {
      return curve switch
      {
        Polyline polyline => PolylineToNative(polyline),
        Polycurve polycurve => PolycurveToNative(polycurve),
        _ => null
      };
    }

    public static ElementShape.PolylineSegment? CurveSegmentToNative(ICurve curve)
    {
      return curve switch
      {
        Line line => LineToNative(line),
        Arc arc => ArcToNative(arc),
        _ => throw new SpeckleException("Archicad Element Shapes can only be created with Lines or Arcs.")
      };
    }

    public static ElementShape PolycurvesToElementShape(ICurve outline, List<ICurve> voids = null)
    {
      var shape = new ElementShape(CurveToNative(outline));
      if (voids?.Count > 0)
        shape.holePolylines = new List<ElementShape.Polyline>(voids.Select(CurveToNative));

      return shape;
    }
  }
}
