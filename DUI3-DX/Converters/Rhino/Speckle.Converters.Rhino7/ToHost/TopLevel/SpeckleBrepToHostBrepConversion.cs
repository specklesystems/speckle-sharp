using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

public class SpeckleBrepToHostBrepConversion : SpeckleToHostGeometryBaseConversion<SOG.Brep, RG.Brep>
{
  public SpeckleBrepToHostBrepConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Brep, RG.Brep> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
