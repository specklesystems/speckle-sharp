
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolycurveToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Polycurve, IRhinoPolyCurve>
{
  public PolycurveToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Polycurve, IRhinoPolyCurve> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}
