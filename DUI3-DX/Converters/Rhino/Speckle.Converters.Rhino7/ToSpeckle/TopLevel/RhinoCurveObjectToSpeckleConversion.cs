using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(CurveObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RhinoCurveObjectToSpeckleConversion : RhinoObjectToSpeckleConverter<CurveObject, RG.Curve, Base>
{
  public RhinoCurveObjectToSpeckleConversion(IRawConversion<RG.Curve, Base> conversion)
    : base(conversion) { }

  protected override RG.Curve GetTypedGeometry(CurveObject input) => input.CurveGeometry;
}
