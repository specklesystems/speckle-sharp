using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToHostTopLevelConverter
  : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Polyline, IRhinoPolylineCurve>
{
  public PolylineToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Polyline, IRhinoPolylineCurve> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}
