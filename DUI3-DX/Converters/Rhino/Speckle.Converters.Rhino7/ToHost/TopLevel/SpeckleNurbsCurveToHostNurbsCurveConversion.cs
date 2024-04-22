using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Curve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpeckleNurbsCurveToHostNurbsCurveConversion : SpeckleToHostGeometryBaseConversion<SOG.Curve, RG.NurbsCurve>
{
  public SpeckleNurbsCurveToHostNurbsCurveConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Curve, RG.NurbsCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
