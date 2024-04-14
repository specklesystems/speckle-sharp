using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(RG.ArcCurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class RhinoArcCurveToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.ArcCurve, Base> // POC: Must return base because an ArcCurve can be an arc or a circle.
{
  public RhinoArcCurveToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.ArcCurve, Base> converter
  )
    : base(contextStack, converter) { }
}
