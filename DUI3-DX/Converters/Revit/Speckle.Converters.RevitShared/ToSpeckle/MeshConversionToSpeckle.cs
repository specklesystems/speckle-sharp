using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(List<DB.Mesh>), 0)]
public class MeshConversionToSpeckle : BaseConversionToSpeckle<List<DB.Mesh>, List<SOG.Mesh>>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.XYZ, Point> _xyzToPointConverter;
  private readonly MeshDataTriangulator _meshDataTriangulator;

  public MeshConversionToSpeckle(
    RevitConversionContextStack contextStack,
    IRawConversion<DB.XYZ, Point> xyzToPointConverter,
    MeshDataTriangulator meshDataTriangulator
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _meshDataTriangulator = meshDataTriangulator;
  }

  public override List<SOG.Mesh> RawConvert(List<DB.Mesh> target)
  {
    MeshBuildHelper buildHelper = new();

    foreach (var mesh in target)
    {
      var revitMaterial = _contextStack.Current.Document.Document.GetElement(mesh.MaterialElementId) as DB.Material;
      Mesh speckleMesh = buildHelper.GetOrCreateMesh(revitMaterial, _contextStack.Current.SpeckleUnits);
      _meshDataTriangulator.Triangulate(mesh, speckleMesh.faces, speckleMesh.vertices);
    }

    return buildHelper.GetAllValidMeshes();
  }
}
