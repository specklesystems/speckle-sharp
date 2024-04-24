using ArcGIS.Core.CIM;

namespace Speckle.Converters.ArcGIS3.Geometry;

public interface IGeometryUtils
{
  public bool ValidateMesh(SOG.Mesh mesh);

  public int RGBToInt(CIMRGBColor color);

  public int CIMColorToInt(CIMColor color);

  public bool IsClockwisePolygon(SOG.Polyline polyline);
}
