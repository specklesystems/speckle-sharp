using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.AutocadShared.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Curve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Curve, ADB.Curve>
{
  private readonly ITypedConverter<SOG.Curve, AG.NurbCurve3d> _curveConverter;

  public CurveToHostConverter(ITypedConverter<SOG.Curve, AG.NurbCurve3d> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Curve)target);

  public ADB.Curve RawConvert(SOG.Curve target) => ADB.Curve.CreateFromGeCurve(_curveConverter.RawConvert(target));
}
