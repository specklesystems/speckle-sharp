using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(IRhinoCurveObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveObjectToSpeckleTopLevelConverter
  : RhinoObjectToSpeckleTopLevelConverter<IRhinoCurveObject, IRhinoCurve, Base>
{
  public CurveObjectToSpeckleTopLevelConverter(ITypedConverter<IRhinoCurve, Base> conversion)
    : base(conversion) { }

  protected override IRhinoCurve GetTypedGeometry(IRhinoCurveObject input) => input.CurveGeometry;
}
