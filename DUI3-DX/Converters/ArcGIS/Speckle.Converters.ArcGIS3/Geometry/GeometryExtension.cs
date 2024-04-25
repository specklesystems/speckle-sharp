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

  public static SOG.Mesh CreateDisplayMeshForPolygon3d(this SGIS.GisPolygonGeometry3d polygon3d)
  {
    // POC: todo
    throw new SpeckleConversionException("TODO");
  }

  public static SOG.Mesh CreateDisplayMeshForMultipatch(this SGIS.GisMultipatchGeometry multipatch)
  {
    // POC: todo
    throw new SpeckleConversionException("TODO");
  }

  public static SGIS.GisMultipatchGeometry CompleteMultipatchTriangleStrip(
    this ACG.Multipatch target,
    List<List<SOG.Point>> allPoints,
    int idx
  )
  {
    SGIS.GisMultipatchGeometry multipatch = new();
    int count;
    List<double> pointCoords = allPoints[idx].SelectMany(x => new List<double>() { x.x, x.y, x.z }).ToList();

    // get data for this multipatch part
    int ptCount = target.GetPatchPointCount(idx);

    for (int ptIdx = 0; ptIdx < ptCount; ptIdx++)
    {
      count = ptIdx + 1;
      if (count > 2) // every new point adds a triangle
      {
        multipatch.faces.AddRange(new List<int>() { 3, count - 3, count - 2, count - 1 });
        multipatch.vertices.AddRange(pointCoords.GetRange(count - 3, 3).ToList());
        pointCoords.RemoveAt(0);
      }
    }
    return multipatch;
  }

  public static SGIS.GisMultipatchGeometry CompleteMultipatchTriangles(
    this ACG.Multipatch target,
    List<List<SOG.Point>> allPoints,
    int idx
  )
  {
    SGIS.GisMultipatchGeometry multipatch = new();
    int count;
    List<double> pointCoords = allPoints[idx].SelectMany(x => new List<double>() { x.x, x.y, x.z }).ToList();

    // get data for this multipatch part
    int ptCount = target.GetPatchPointCount(idx);

    for (int ptIdx = 0; ptIdx < ptCount; ptIdx++)
    {
      var convertedPt = allPoints[idx][ptIdx];
      pointCoords.AddRange(new List<double>() { convertedPt.x, convertedPt.y, convertedPt.z });
      count = ptIdx + 1;
      if (count > 2 && count % 3 == 0) // every 3 new points is a new triangle
      {
        multipatch.faces.AddRange(new List<int>() { 3, count - 3, count - 2, count - 1 });
        multipatch.vertices.AddRange(pointCoords.GetRange(count - 3, 3).ToList());
        pointCoords.Clear();
      }
    }
    return multipatch;
  }

  public static SGIS.GisMultipatchGeometry CompleteMultipatchTriangleFan(
    this ACG.Multipatch target,
    List<List<SOG.Point>> allPoints,
    int idx
  )
  {
    SGIS.GisMultipatchGeometry multipatch = new();
    int count;
    List<double> pointCoords = allPoints[idx].SelectMany(x => new List<double>() { x.x, x.y, x.z }).ToList();

    // get data for this multipatch part
    int ptCount = target.GetPatchPointCount(idx);

    for (int ptIdx = 0; ptIdx < ptCount; ptIdx++)
    {
      var convertedPt = allPoints[idx][ptIdx];
      pointCoords.AddRange(new List<double>() { convertedPt.x, convertedPt.y, convertedPt.z });
      count = ptIdx + 1;
      if (count > 2) // every new point adds a triangle (originates from 0)
      {
        multipatch.faces.AddRange(new List<int>() { 3, 0, count - 2, count - 1 });
        multipatch.vertices.AddRange(pointCoords.GetRange(count - 3, 3).ToList());
        pointCoords.RemoveAt(0);
      }
    }
    return multipatch;
  }
}
