using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.AutocadShared.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Curve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Curve, ADB.Curve>
{
  private readonly IRawConversion<SOG.Curve, AG.NurbCurve3d> _curveConverter;
  public CurveToHostConverter(IRawConversion<SOG.Curve, AG.NurbCurve3d> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Curve)target);

  public ADB.Curve RawConvert(SOG.Curve target)
  {
    return ADB.Curve.CreateFromGeCurve(_curveConverter.RawConvert(target));
  }
}
