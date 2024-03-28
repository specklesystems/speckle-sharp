using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.XYZ), 0)]
public class XyzConversionToPoint : BaseConversionToSpeckle<DB.XYZ, SOG.Point>
{
  private readonly ToSpeckleScalingService _toSpeckleScalingService;
  private readonly RevitConversionContextStack _contextStack;

  public XyzConversionToPoint(ToSpeckleScalingService toSpeckleScalingService, RevitConversionContextStack contextStack)
  {
    _toSpeckleScalingService = toSpeckleScalingService;
    _contextStack = contextStack;
  }

  public override SOG.Point RawConvert(DB.XYZ target)
  {
    var pointToSpeckle = new SOG.Point(
      _toSpeckleScalingService.ScaleLength(target.X),
      _toSpeckleScalingService.ScaleLength(target.Y),
      _toSpeckleScalingService.ScaleLength(target.Z),
      _contextStack.Current.SpeckleUnits
    );
    return pointToSpeckle;
  }
}
