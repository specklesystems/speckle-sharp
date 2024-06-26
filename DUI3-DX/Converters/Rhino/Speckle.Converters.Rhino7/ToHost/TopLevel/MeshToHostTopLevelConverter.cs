using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Mesh, IRhinoMesh>
{
  public MeshToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Mesh, IRhinoMesh> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}
