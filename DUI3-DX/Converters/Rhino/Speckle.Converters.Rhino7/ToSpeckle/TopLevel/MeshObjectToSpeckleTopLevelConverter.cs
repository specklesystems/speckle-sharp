using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(IRhinoMeshObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshObjectToSpeckleTopLevelConverter
  : RhinoObjectToSpeckleTopLevelConverter<IRhinoMeshObject, IRhinoMesh, SOG.Mesh>
{
  public MeshObjectToSpeckleTopLevelConverter(ITypedConverter<IRhinoMesh, SOG.Mesh> conversion)
    : base(conversion) { }

  protected override IRhinoMesh GetTypedGeometry(IRhinoMeshObject input) => input.MeshGeometry;
}
