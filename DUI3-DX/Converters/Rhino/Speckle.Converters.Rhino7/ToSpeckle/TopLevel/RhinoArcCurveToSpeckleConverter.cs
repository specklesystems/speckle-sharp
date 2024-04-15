using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(RG.ArcCurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class RhinoArcCurveToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.ArcCurve, Base> // POC: CNX-9275 Must return base because an ArcCurve can be an arc or a circle, and ICurve/Base are not related so can't do ICurve here.
{
  public RhinoArcCurveToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.ArcCurve, Base> converter
  )
    : base(contextStack, converter) { }
}
