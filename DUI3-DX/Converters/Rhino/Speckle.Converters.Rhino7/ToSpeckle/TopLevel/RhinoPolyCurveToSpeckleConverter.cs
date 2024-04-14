using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(RG.PolyCurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class RhinoPolyCurveToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.PolyCurve, SOG.Polycurve>
{
  public RhinoPolyCurveToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.PolyCurve, SOG.Polycurve> converter
  )
    : base(contextStack, converter) { }
}
