using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

public sealed class RhinoNurbsCurveToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.NurbsCurve, SOG.Curve>
{
  public RhinoNurbsCurveToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.NurbsCurve, SOG.Curve> converter
  )
    : base(contextStack, converter) { }
}
