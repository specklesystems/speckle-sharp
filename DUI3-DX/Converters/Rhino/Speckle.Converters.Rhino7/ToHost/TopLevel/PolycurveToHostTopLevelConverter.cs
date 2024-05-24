using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolycurveToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Polycurve, RG.PolyCurve>
{
  public PolycurveToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<SOG.Polycurve, RG.PolyCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
