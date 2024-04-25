using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

/// <summary>
/// Solid conversion is a one->many. For each material used in the solid, a mesh will be returned to reduce the amount of instances created.
/// </summary>
public class SolidConversionToSpeckle : IRawConversion<DB.Solid, List<SOG.Mesh>>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly IRawConversion<Dictionary<DB.ElementId, List<DB.Mesh>>, List<SOG.Mesh>> _meshByMaterialConverter;

  public SolidConversionToSpeckle(
    RevitConversionContextStack contextStack,
    IRawConversion<Dictionary<DB.ElementId, List<DB.Mesh>>, List<SOG.Mesh>> meshByMaterialConverter
  )
  {
    _contextStack = contextStack;
    _meshByMaterialConverter = meshByMaterialConverter;
  }

  /// <summary>
  /// Converts the input <see cref="DB.Solid"/> object into a list of <see cref="SOG.Mesh"/>.
  /// </summary>
  /// <param name="target">The input <see cref="DB.Solid"/> object to be converted.</param>
  /// <returns>
  /// A list of <see cref="SOG.Mesh"/> objects that represent the input <see cref="DB.Solid"/> object. Each mesh in the list corresponds to a different material in the original solid.
  /// </returns>
  /// <remarks>
  /// This conversion process first triangulates the input solid by material, and then converts the result to raw meshes individually.
  /// Be aware that this operation might be computationally intensive for complex solids, due to the need for triangulation.
  /// </remarks>
  public List<SOG.Mesh> RawConvert(DB.Solid target)
  {
    var meshesByMaterial = GetTriangulatedMeshesFromSolidByMaterial(target);
    return _meshByMaterialConverter.RawConvert(meshesByMaterial);
  }

  private Dictionary<DB.ElementId, List<DB.Mesh>> GetTriangulatedMeshesFromSolidByMaterial(DB.Solid solid)
  {
    var result = new Dictionary<DB.ElementId, List<DB.Mesh>>();
    foreach (DB.Face face in solid.Faces)
    {
      if (!result.ContainsKey(face.MaterialElementId))
      {
        result[face.MaterialElementId] = new List<DB.Mesh>();
      }

      result[face.MaterialElementId].Add(face.Triangulate());
    }

    return result;
  }
}
