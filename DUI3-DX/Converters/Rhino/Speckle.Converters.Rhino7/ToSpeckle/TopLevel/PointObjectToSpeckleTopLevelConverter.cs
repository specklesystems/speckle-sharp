using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(PointObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointObjectToSpeckleTopLevelTopLevelConverter
  : RhinoObjectToSpeckleTopLevelConverter<PointObject, RG.Point, SOG.Point>
{
  public PointObjectToSpeckleTopLevelTopLevelConverter(IRawConversion<RG.Point, SOG.Point> conversion)
    : base(conversion) { }

  protected override RG.Point GetTypedGeometry(PointObject input) => input.PointGeometry;
}
