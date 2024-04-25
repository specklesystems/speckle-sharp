using ArcGIS.Core.CIM;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry;

public static class GeometryUtils
{
  public static bool ValidateMesh(this SOG.Mesh mesh)
  {
    if (mesh.vertices.Count < 3)
    {
      return false;
    }
    else if (mesh.faces.Count < 4)
    {
      return false;
    }
    return true;
  }

  public static int RGBToInt(this CIMRGBColor color)
  {
    return (255 << 24) | ((int)Math.Round(color.R) << 16) | ((int)Math.Round(color.G) << 8) | (int)Math.Round(color.B);
  }

  public static int CIMColorToInt(this CIMColor color)
  {
    return (255 << 24)
      | ((int)Math.Round(color.Values[0]) << 16)
      | ((int)Math.Round(color.Values[1]) << 8)
      | (int)Math.Round(color.Values[2]);
  }

  public static bool IsClockwisePolygon(this SOG.Polyline polyline)
  {
    bool isClockwise;
    double sum = 0;

    List<SOG.Point> points = polyline.GetPoints();

    if (points.Count < 3)
    {
      throw new ArgumentException("Not enough points for polygon orientation check");
    }
    if (points[0] != points[^1])
    {
      points.Add(points[0]);
    }

    for (int i = 0; i < points.Count - 1; i++)
    {
      sum += (points[i + 1].x - points[i].x) * (points[i + 1].y + points[i].y);
    }
    isClockwise = sum > 0;
    return isClockwise;
  }

  public static SOG.Mesh CreateDisplayMeshForPolygon(this SGIS.GisPolygonGeometry polygon)
  {
    if (polygon.voids.Count == 0)
    {
      // ensure counter-clockwise orientation for up-facing mesh faces
      bool isClockwise = polygon.boundary.IsClockwisePolygon();
      List<SOG.Point> boundaryPts = polygon.boundary.GetPoints();
      if (isClockwise)
      {
        boundaryPts.Reverse();
      }

      // generate Mesh
      int ptCount = boundaryPts.Count;
      List<int> faces = new() { ptCount };
      faces.AddRange(Enumerable.Range(0, ptCount).ToList());

      return new SOG.Mesh(boundaryPts.SelectMany(x => new List<double> { x.x, x.y, x.z }).ToList(), faces);
    }
    else
    {
      throw new SpeckleConversionException("Cannot generate display value for polygons with voids");
    }
  }
}
