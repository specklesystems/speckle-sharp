using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Point, IRhinoPoint>
{
  public PointToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Point, IRhinoPoint> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}
