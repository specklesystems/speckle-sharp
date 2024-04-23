using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpecklePolycurveToHostPolycurveConversion
  : SpeckleToHostGeometryBaseConversion<SOG.Polycurve, RG.PolyCurve>
{
  public SpecklePolycurveToHostPolycurveConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Polycurve, RG.PolyCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
