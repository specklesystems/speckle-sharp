using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(CurveObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveObjectToSpeckleTopLevelConverter : RhinoObjectToSpeckleTopLevelConverter<CurveObject, RG.Curve, Base>
{
  public CurveObjectToSpeckleTopLevelConverter(ITypedConverter<RG.Curve, Base> conversion)
    : base(conversion) { }

  protected override RG.Curve GetTypedGeometry(CurveObject input) => input.CurveGeometry;
}
