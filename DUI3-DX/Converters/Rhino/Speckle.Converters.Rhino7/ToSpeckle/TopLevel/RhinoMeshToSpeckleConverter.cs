using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(RG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class RhinoMeshToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.Mesh, SOG.Mesh>
{
  public RhinoMeshToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.Mesh, SOG.Mesh> converter
  )
    : base(contextStack, converter) { }
}
