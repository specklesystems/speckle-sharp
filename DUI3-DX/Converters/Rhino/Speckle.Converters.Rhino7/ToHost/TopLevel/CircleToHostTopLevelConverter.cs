using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Circle, IRhinoArcCurve>
{
  public CircleToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Circle, IRhinoArcCurve> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}
