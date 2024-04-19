using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpeckleArcToHostArcCurveConversion : SpeckleToHostGeometryBaseConversion<SOG.Arc, RG.ArcCurve>
{
  public SpeckleArcToHostArcCurveConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Arc, RG.ArcCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
