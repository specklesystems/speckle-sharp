using Objects.Other;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class MeshConversionToSpeckle : ITypedConverter<IRevitMesh, SOG.Mesh>
{
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<IRevitMaterial, RenderMaterial> _materialConverter;
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;

  public MeshConversionToSpeckle(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<IRevitMaterial, RenderMaterial> materialConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _materialConverter = materialConverter;
  }

  public SOG.Mesh Convert(IRevitMesh target)
  {
    var doc = _contextStack.Current.Document;

    List<double> vertices = GetSpeckleMeshVertexData(target);
    List<int> faces = GetSpeckleMeshFaceData(target);

    var element = doc.GetElement(target.MaterialElementId);
    var revitMaterial = element?.ToMaterial();
    RenderMaterial? speckleMaterial = null;
    if (revitMaterial is not null)
    {
      speckleMaterial = _materialConverter.Convert(revitMaterial);
    }

    return new SOG.Mesh(vertices, faces, units: _contextStack.Current.SpeckleUnits)
    {
      ["renderMaterial"] = speckleMaterial
    };
  }

  private List<double> GetSpeckleMeshVertexData(IRevitMesh target)
  {
    var vertices = new List<double>(target.Vertices.Count * 3);

    foreach (var vert in target.Vertices)
    {
      vertices.AddRange(_xyzToPointConverter.Convert(vert).ToList());
    }

    return vertices;
  }

  private List<int> GetSpeckleMeshFaceData(IRevitMesh target)
  {
    var faces = new List<int>(target.NumTriangles * 4);
    for (int i = 0; i < target.NumTriangles; i++)
    {
      var triangle = target.GetTriangle(i);
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
  private IReadOnlyList<int> GetMeshTriangleData(IRevitMeshTriangle triangle) =>
    new[]
    {
      3, // The TRIANGLE flag in speckle
      (int)triangle.GetIndex(0),
      (int)triangle.GetIndex(1),
      (int)triangle.GetIndex(2)
    };
}
