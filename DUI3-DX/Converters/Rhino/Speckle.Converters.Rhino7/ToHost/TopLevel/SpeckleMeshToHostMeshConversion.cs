using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

public class SpeckleMeshToHostMeshConversion : SpeckleToHostGeometryBaseConversion<SOG.Mesh, RG.Mesh>
{
  public SpeckleMeshToHostMeshConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Mesh, RG.Mesh> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
