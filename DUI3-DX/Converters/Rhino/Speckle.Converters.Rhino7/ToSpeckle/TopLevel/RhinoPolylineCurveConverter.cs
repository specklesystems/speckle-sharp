using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

public sealed class RhinoPolylineCurveConverter : HostToSpeckleGeometryBaseConversion<RG.PolylineCurve, SOG.Polyline>
{
  public RhinoPolylineCurveConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.PolylineCurve, SOG.Polyline> converter
  )
    : base(contextStack, converter) { }
}
