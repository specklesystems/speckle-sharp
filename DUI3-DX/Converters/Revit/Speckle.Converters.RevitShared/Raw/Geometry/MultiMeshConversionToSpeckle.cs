using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class MultiMeshConversionToSpeckle : IRawConversion<List<DB.Mesh>, List<SOG.Mesh>>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly MeshDataTriangulator _meshDataTriangulator;

  public MultiMeshConversionToSpeckle(
    RevitConversionContextStack contextStack,
    MeshDataTriangulator meshDataTriangulator
  )
  {
    _contextStack = contextStack;
    _meshDataTriangulator = meshDataTriangulator;
  }

  public List<SOG.Mesh> RawConvert(List<DB.Mesh> target)
  {
    // POC: should be injected
    MeshBuildHelper buildHelper = new();

    foreach (var mesh in target)
    {
      var revitMaterial = (DB.Material)_contextStack.Current.Document.Document.GetElement(mesh.MaterialElementId);
      SOG.Mesh speckleMesh = buildHelper.GetOrCreateMesh(revitMaterial, _contextStack.Current.SpeckleUnits);
      _meshDataTriangulator.Triangulate(mesh, speckleMesh.faces, speckleMesh.vertices);
    }

    return buildHelper.GetAllValidMeshes();
  }
}
