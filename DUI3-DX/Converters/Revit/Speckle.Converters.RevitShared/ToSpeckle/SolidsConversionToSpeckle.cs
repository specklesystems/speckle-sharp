using System.Collections.Generic;
using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Mesh = Objects.Geometry.Mesh;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(List<DB.Solid>), 0)]
public class SolidsConversionToSpeckle : BaseConversionToSpeckle<List<DB.Solid>, List<Mesh>>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly MeshDataTriangulator _meshDataTriangulator;

  public override List<Mesh> RawConvert(List<DB.Solid> target)
  {
    MeshBuildHelper meshBuildHelper = new();

    var MeshMap = new Dictionary<Mesh, List<DB.Mesh>>();
    foreach (Solid solid in target)
    {
      foreach (Face face in solid.Faces)
      {
        DB.Material faceMaterial =
          _contextStack.Current.Document.Document.GetElement(face.MaterialElementId) as DB.Material;
        Mesh m = meshBuildHelper.GetOrCreateMesh(faceMaterial, _contextStack.Current.SpeckleUnits);
        if (!MeshMap.ContainsKey(m))
        {
          MeshMap.Add(m, new List<DB.Mesh>());
        }
        MeshMap[m].Add(face.Triangulate());
      }
    }

    foreach (var meshData in MeshMap)
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
