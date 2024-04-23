using Objects.Other;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class SingleMeshConversionToSpeckle : IRawConversion<DB.Mesh, SOG.Mesh>
{
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly IRawConversion<DB.Material, RenderMaterial> _materialConverter;
  private readonly RevitConversionContextStack _contextStack;

  public SingleMeshConversionToSpeckle(
    RevitConversionContextStack contextStack,
    IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter,
    IRawConversion<DB.Material, RenderMaterial> materialConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _materialConverter = materialConverter;
  }

  public SOG.Mesh RawConvert(DB.Mesh target)
  {
    var doc = _contextStack.Current.Document.Document;

    List<double> vertices = GetSpeckleMeshVertexData(target);
    List<int> faces = GetSpeckleMeshFaceData(target);

    RenderMaterial? speckleMaterial = null;
    if (doc.GetElement(target.MaterialElementId) is DB.Material revitMaterial)
    {
      speckleMaterial = _materialConverter.RawConvert(revitMaterial);
    }

    return new SOG.Mesh(vertices, faces, units: _contextStack.Current.SpeckleUnits)
    {
      ["renderMaterial"] = speckleMaterial
    };
  }

  private List<double> GetSpeckleMeshVertexData(DB.Mesh target)
  {
    var vertices = new List<double>();

    foreach (var vert in target.Vertices)
    {
      vertices.AddRange(_xyzToPointConverter.RawConvert(vert).ToList());
    }

    return vertices;
  }

  private List<int> GetSpeckleMeshFaceData(DB.Mesh target)
  {
    var faces = new List<int>();
    for (int i = 0; i < target.NumTriangles; i++)
    {
      var triangle = target.get_Triangle(i);
      faces.AddRange(GetMeshTriangleData(triangle));
    }

    return faces;
  }

  /// <summary>
  /// Retrieves the triangle data of a mesh to be stored in a Speckle Mesh faces property.
  /// </summary>
  /// <param name="triangle">The mesh triangle object.</param>
  /// <returns>A list of integers representing the triangle data.</returns>
  /// <remarks>
  /// Output format is a 4 item list with format [3, v1, v2, v3]; where the first item is the triangle flag (for speckle)
  /// and the 3 following numbers are the indices of each vertex in the vertex list.
  /// </remarks>
  private IReadOnlyList<int> GetMeshTriangleData(DB.MeshTriangle triangle) =>
    new[]
    {
      3, // The TRIANGLE flag in speckle
      (int)triangle.get_Index(0),
      (int)triangle.get_Index(1),
      (int)triangle.get_Index(2)
    };
}
