using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class XyzConversionToPoint : IRawConversion<DB.XYZ, SOG.Point>
{
  private readonly ToSpeckleScalingService _toSpeckleScalingService;
  private readonly RevitConversionContextStack _contextStack;

  public XyzConversionToPoint(ToSpeckleScalingService toSpeckleScalingService, RevitConversionContextStack contextStack)
  {
    _toSpeckleScalingService = toSpeckleScalingService;
    _contextStack = contextStack;
  }

  public SOG.Point RawConvert(DB.XYZ target)
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
