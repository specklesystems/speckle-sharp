using Archicad.Model;
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
  }
}
