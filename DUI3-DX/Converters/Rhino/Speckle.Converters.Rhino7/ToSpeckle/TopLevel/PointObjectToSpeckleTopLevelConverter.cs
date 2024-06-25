using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(IRhinoPointObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointObjectToSpeckleTopLevelConverter
  : RhinoObjectToSpeckleTopLevelConverter<IRhinoPointObject, IRhinoPoint, SOG.Point>
{
  public PointObjectToSpeckleTopLevelConverter(ITypedConverter<IRhinoPoint, SOG.Point> conversion)
    : base(conversion) { }

  protected override IRhinoPoint GetTypedGeometry(IRhinoPointObject input) => input.PointGeometry;
}
