using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(RG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class RhinoPointToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.Point, SOG.Point>
{
  public RhinoPointToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.Point, SOG.Point> converter
  )
    : base(contextStack, converter) { }
}
