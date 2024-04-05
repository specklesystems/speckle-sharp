using Objects.Geometry;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.Helpers;

public sealed class MeshDataTriangulator
{
  private readonly IRawConversion<DB.XYZ, Point> _xyzToPointConverter;

  public MeshDataTriangulator(IRawConversion<DB.XYZ, Point> xyzToPointConverter)
  {
    _xyzToPointConverter = xyzToPointConverter;
  }

  /// <summary>
  /// Given <paramref name="mesh"/>, will convert and add triangle data to <paramref name="faces"/> and <paramref name="vertices"/>
  /// </summary>
  /// <param name="mesh">The revit mesh to convert</param>
  /// <param name="faces">The faces list to add to</param>
  /// <param name="vertices">The vertices list to add to</param>
  public void Triangulate(DB.Mesh mesh, List<int> faces, List<double> vertices)
  {
    int faceIndexOffset = vertices.Count / 3;

    foreach (var vert in mesh.Vertices)
    {
      var (x, y, z) = _xyzToPointConverter.RawConvert(vert);
      vertices.Add(x);
      vertices.Add(y);
      vertices.Add(z);
    }

    for (int i = 0; i < mesh.NumTriangles; i++)
    {
      var triangle = mesh.get_Triangle(i);

      faces.Add(3); // TRIANGLE flag
      faces.Add((int)triangle.get_Index(0) + faceIndexOffset);
      faces.Add((int)triangle.get_Index(1) + faceIndexOffset);
      faces.Add((int)triangle.get_Index(2) + faceIndexOffset);
    }
  }
}
