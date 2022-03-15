using System.Linq;
using Archicad.Model;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Speckle.Core.Kits;

namespace Archicad.Converters
{
  public static class Utils
  {
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

      return new MeshModel.Vertex() { x = point.x * scale, y = point.y * scale, z = point.z * scale };
    }

    public static Polycurve PolycurveToNative(ElementShape.Polyline archiPolyline)
    {
      var poly = new Polycurve
      {
        units = Units.Meters,
        closed = archiPolyline.polylineSegments.First().startPoint == archiPolyline.polylineSegments.Last().endPoint
      };
      foreach ( var segment in archiPolyline.polylineSegments )
      {
        poly.segments.Add(segment.arcAngle == 0 ? new Line(segment.startPoint, segment.endPoint) : new Arc(segment.startPoint, segment.endPoint, segment.arcAngle));
      }

      return poly;
    }
  }
}
