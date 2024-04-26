using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpeckleEllipseToHostNurbsCurveConversion : SpeckleToHostGeometryBaseConversion<SOG.Ellipse, RG.NurbsCurve>
{
  public SpeckleEllipseToHostNurbsCurveConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Ellipse, RG.NurbsCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
