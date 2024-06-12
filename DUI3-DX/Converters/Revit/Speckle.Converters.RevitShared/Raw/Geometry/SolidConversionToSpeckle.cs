using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

/// <summary>
/// Solid conversion is a one->many. For each material used in the solid, a mesh will be returned to reduce the amount of instances created.
/// </summary>
public class SolidConversionToSpeckle : ITypedConverter<IRevitSolid, List<SOG.Mesh>>
{
  private readonly ITypedConverter<
    Dictionary<IRevitElementId, List<IRevitMesh>>,
    List<SOG.Mesh>
  > _meshByMaterialConverter;

  public SolidConversionToSpeckle(
    ITypedConverter<Dictionary<IRevitElementId, List<IRevitMesh>>, List<SOG.Mesh>> meshByMaterialConverter
  )
  {
    _meshByMaterialConverter = meshByMaterialConverter;
  }

  /// <summary>
  /// Converts the input <see cref="Speckle.Revit.Interfaces.IRevitSolid"/> object into a list of <see cref="Objects.Geometry.Mesh"/>.
  /// </summary>
  /// <param name="target">The input <see cref="Speckle.Revit.Interfaces.IRevitSolid"/> object to be converted.</param>
  /// <returns>
  /// A list of <see cref="Objects.Geometry.Mesh"/> objects that represent the input <see cref="Speckle.Revit.Interfaces.IRevitSolid"/> object. Each mesh in the list corresponds to a different material in the original solid.
  /// </returns>
  /// <remarks>
  /// This conversion process first triangulates the input solid by material, and then converts the result to raw meshes individually.
  /// Be aware that this operation might be computationally intensive for complex solids, due to the need for triangulation.
  /// </remarks>
  public List<SOG.Mesh> Convert(IRevitSolid target)
  {
    var meshesByMaterial = GetTriangulatedMeshesFromSolidByMaterial(target);
    return _meshByMaterialConverter.Convert(meshesByMaterial);
  }

  private Dictionary<IRevitElementId, List<IRevitMesh>> GetTriangulatedMeshesFromSolidByMaterial(IRevitSolid solid)
  {
    var result = new Dictionary<IRevitElementId, List<IRevitMesh>>();
    foreach (IRevitFace face in solid.Faces)
    {
      if (!result.TryGetValue(face.MaterialElementId, out var mat))
      {
        mat = new List<IRevitMesh>();
        result[face.MaterialElementId] = mat;
      }
      mat.Add(face.Triangulate());
    }

    return result;
  }
}
