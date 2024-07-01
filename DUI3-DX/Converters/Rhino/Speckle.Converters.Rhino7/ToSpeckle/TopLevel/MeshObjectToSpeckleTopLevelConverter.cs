using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(MeshObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshObjectToSpeckleTopLevelConverter : RhinoObjectToSpeckleTopLevelConverter<MeshObject, RG.Mesh, SOG.Mesh>
{
  public MeshObjectToSpeckleTopLevelConverter(ITypedConverter<RG.Mesh, SOG.Mesh> conversion)
    : base(conversion) { }

  protected override RG.Mesh GetTypedGeometry(MeshObject input) => input.MeshGeometry;
}
