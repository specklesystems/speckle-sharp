using Objects.Other;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class MeshByMaterialDictionaryToSpeckle
  : ITypedConverter<Dictionary<IRevitElementId, List<IRevitMesh>>, List<SOG.Mesh>>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<IRevitMaterial, RenderMaterial> _materialConverter;

  public MeshByMaterialDictionaryToSpeckle(
    ITypedConverter<IRevitMaterial, RenderMaterial> materialConverter,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter
  )
  {
    _materialConverter = materialConverter;
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
  }

  /// <summary>
  /// Converts a dictionary of Revit meshes, where key is MaterialId, into a list of Speckle meshes.
  /// </summary>
  /// <param name="target">A dictionary with IRevitElementId keys and List of IRevitMesh values.</param>
  /// <returns>
  /// Returns a list of <see cref="SOG.Mesh"/> objects where each mesh represents one unique material in the input dictionary.
  /// </returns>
  /// <remarks>
  /// Be aware that this method internally creates a new instance of <see cref="SOG.Mesh"/> for each unique material in the input dictionary.
  /// These meshes are created with an initial capacity based on the size of the vertex and face arrays to avoid unnecessary resizing.
  /// Also note that, for each unique material, the method tries to retrieve the related IRevitMaterial from the current document and convert it. If the conversion is successful,
  /// the material is added to the corresponding Speckle mesh. If the conversion fails, the operation simply continues without the material.
  /// </remarks>
  public List<SOG.Mesh> Convert(Dictionary<IRevitElementId, List<IRevitMesh>> target)
  {
    var result = new List<SOG.Mesh>(target.Keys.Count);

    foreach (var meshData in target)
    {
      IRevitElementId materialId = meshData.Key;
      List<IRevitMesh> meshes = meshData.Value;

      // We compute the final size of the arrays to prevent unnecessary resizing.
      (int verticesSize, int facesSize) = GetVertexAndFaceListSize(meshes);

      // Initialise a new empty mesh with units and material
      var speckleMesh = new SOG.Mesh(
        new List<double>(verticesSize),
        new List<int>(facesSize),
        units: _contextStack.Current.SpeckleUnits
      );

      var doc = _contextStack.Current.Document;
      if (doc.GetElement(materialId) is IRevitMaterial material)
      {
        speckleMesh["renderMaterial"] = _materialConverter.Convert(material);
      }

      // Append the revit mesh data to the speckle mesh
      foreach (var mesh in meshes)
      {
        AppendToSpeckleMesh(mesh, speckleMesh);
      }

      result.Add(speckleMesh);
    }

    return result;
  }

  private void AppendToSpeckleMesh(IRevitMesh mesh, SOG.Mesh speckleMesh)
  {
    int faceIndexOffset = speckleMesh.vertices.Count / 3;

    foreach (var vert in mesh.Vertices)
    {
      var (x, y, z) = _xyzToPointConverter.Convert(vert);
      speckleMesh.vertices.Add(x);
      speckleMesh.vertices.Add(y);
      speckleMesh.vertices.Add(z);
    }

    for (int i = 0; i < mesh.NumTriangles; i++)
    {
      var triangle = mesh.GetTriangle(i);

      speckleMesh.faces.Add(3); // TRIANGLE flag
      speckleMesh.faces.Add((int)triangle.GetIndex(0) + faceIndexOffset);
      speckleMesh.faces.Add((int)triangle.GetIndex(1) + faceIndexOffset);
      speckleMesh.faces.Add((int)triangle.GetIndex(2) + faceIndexOffset);
    }
  }

  private static (int vertexCount, int) GetVertexAndFaceListSize(List<IRevitMesh> meshes)
  {
    int numberOfVertices = 0;
    int numberOfFaces = 0;
    foreach (var mesh in meshes)
    {
      if (mesh == null)
      {
        continue;
      }

      numberOfVertices += mesh.Vertices.Count * 3;
      numberOfFaces += mesh.NumTriangles * 4;
    }

    return (numberOfVertices, numberOfFaces);
  }
}
