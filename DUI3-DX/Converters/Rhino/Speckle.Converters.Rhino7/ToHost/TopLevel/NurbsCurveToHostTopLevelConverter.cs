using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Curve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class NurbsCurveToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Curve, IRhinoNurbsCurve>
{
  public NurbsCurveToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Curve, IRhinoNurbsCurve> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}
