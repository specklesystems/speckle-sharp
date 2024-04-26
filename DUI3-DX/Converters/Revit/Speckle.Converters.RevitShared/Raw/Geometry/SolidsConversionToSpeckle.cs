using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: do we need to have this atm?
// why do we need a List<DB.Solid>, do we encounter these in the converter logic?
// it looks wrong here or perhaps the List<DB.Solid> is coming from the connector?
public class SolidsConversionToSpeckle : IRawConversion<List<DB.Solid>, List<SOG.Mesh>>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly MeshDataTriangulator _meshDataTriangulator;

  public SolidsConversionToSpeckle(RevitConversionContextStack contextStack, MeshDataTriangulator meshDataTriangulator)
  {
    _contextStack = contextStack;
    _meshDataTriangulator = meshDataTriangulator;
  }

  // POC: this is converting and caching and using the cache and some of it is a bit questionable
  public List<SOG.Mesh> RawConvert(List<DB.Solid> target)
  {
    MeshBuildHelper meshBuildHelper = new();

    var meshMap = new Dictionary<SOG.Mesh, List<DB.Mesh>>();
    foreach (DB.Solid solid in target)
    {
      foreach (DB.Face face in solid.Faces)
      {
        // POC: throwing here? Just review. Is it necessary armouring against Revit APU weirdness?
        // do direct cast (DB.Material) instead of as and then no need to throw, it will throw naturally
        // of course if we need to do this then maybe it's valid. So hence, just review.
        DB.Material faceMaterial =
          _contextStack.Current.Document.Document.GetElement(face.MaterialElementId) as DB.Material
          ?? throw new SpeckleConversionException("Unable to cast face's materialElementId element to DB.Material");

        // POC: this logic, relationship between material and mesh, seems wrong
        SOG.Mesh m = meshBuildHelper.GetOrCreateMesh(faceMaterial, _contextStack.Current.SpeckleUnits);
        if (!meshMap.TryGetValue(m, out List<DB.Mesh>? value))
        {
          value = new List<DB.Mesh>();
          meshMap.Add(m, value);
        }

        value.Add(face.Triangulate());
      }
    }

    foreach (var meshData in meshMap)
    {
      //It's cheaper to resize lists manually, since we would otherwise be resizing a lot!
      int numberOfVertices = 0;
      int numberOfFaces = 0;
      foreach (DB.Mesh mesh in meshData.Value)
      {
        if (mesh == null)
        {
          continue;
        }

        numberOfVertices += mesh.Vertices.Count * 3;
        numberOfFaces += mesh.NumTriangles * 4;
      }

      meshData.Key.faces.Capacity = numberOfFaces;
      meshData.Key.vertices.Capacity = numberOfVertices;
      foreach (DB.Mesh mesh in meshData.Value)
      {
        if (mesh == null)
        {
          continue;
        }

        _meshDataTriangulator.Triangulate(mesh, meshData.Key.faces, meshData.Key.vertices);
      }
    }

    return meshBuildHelper.GetAllValidMeshes();
  }
}
