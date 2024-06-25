using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Line, IRhinoLineCurve>
{
  public LineToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Line, IRhinoLineCurve> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}
