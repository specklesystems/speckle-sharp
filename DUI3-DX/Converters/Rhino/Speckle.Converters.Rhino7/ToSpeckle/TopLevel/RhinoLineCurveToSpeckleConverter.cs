using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

public sealed class RhinoLineCurveToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.LineCurve, SOG.Line>
{
  public RhinoLineCurveToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.LineCurve, SOG.Line> converter
  )
    : base(contextStack, converter) { }
}
