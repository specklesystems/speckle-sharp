using Objects.Other;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class MeshByMaterialDictionaryToSpeckle : ITypedConverter<Dictionary<DB.ElementId, List<DB.Mesh>>, List<SOG.Mesh>>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<DB.Material, RenderMaterial> _materialConverter;

  public MeshByMaterialDictionaryToSpeckle(
    ITypedConverter<DB.Material, RenderMaterial> materialConverter,
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter
  )
  {
    _materialConverter = materialConverter;
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
  }

  /// <summary>
  /// Converts a dictionary of Revit meshes, where key is MaterialId, into a list of Speckle meshes.
  /// </summary>
  /// <param name="target">A dictionary with DB.ElementId keys and List of DB.Mesh values.</param>
  /// <returns>
  /// Returns a list of <see cref="SOG.Mesh"/> objects where each mesh represents one unique material in the input dictionary.
  /// </returns>
  /// <remarks>
  /// Be aware that this method internally creates a new instance of <see cref="SOG.Mesh"/> for each unique material in the input dictionary.
  /// These meshes are created with an initial capacity based on the size of the vertex and face arrays to avoid unnecessary resizing.
  /// Also note that, for each unique material, the method tries to retrieve the related DB.Material from the current document and convert it. If the conversion is successful,
  /// the material is added to the corresponding Speckle mesh. If the conversion fails, the operation simply continues without the material.
  /// </remarks>
  public List<SOG.Mesh> Convert(Dictionary<DB.ElementId, List<DB.Mesh>> target)
  {
    var result = new List<SOG.Mesh>(target.Keys.Count);

    foreach (var meshData in target)
    {
      DB.ElementId materialId = meshData.Key;
      List<DB.Mesh> meshes = meshData.Value;

      // We compute the final size of the arrays to prevent unnecessary resizing.
      (int verticesSize, int facesSize) = GetVertexAndFaceListSize(meshes);

      // Initialise a new empty mesh with units and material
      var speckleMesh = new SOG.Mesh(
        new List<double>(verticesSize),
        new List<int>(facesSize),
        units: _contextStack.Current.SpeckleUnits
      );

      var doc = _contextStack.Current.Document;
      if (doc.GetElement(materialId) is DB.Material material)
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

  private void AppendToSpeckleMesh(DB.Mesh mesh, SOG.Mesh speckleMesh)
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
      var triangle = mesh.get_Triangle(i);

      speckleMesh.faces.Add(3); // TRIANGLE flag
      speckleMesh.faces.Add((int)triangle.get_Index(0) + faceIndexOffset);
      speckleMesh.faces.Add((int)triangle.get_Index(1) + faceIndexOffset);
      speckleMesh.faces.Add((int)triangle.get_Index(2) + faceIndexOffset);
    }
  }

  private static (int vertexCount, int) GetVertexAndFaceListSize(List<DB.Mesh> meshes)
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
