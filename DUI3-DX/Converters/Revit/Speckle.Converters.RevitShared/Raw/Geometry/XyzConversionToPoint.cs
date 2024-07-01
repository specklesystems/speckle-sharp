using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class XyzConversionToPoint : ITypedConverter<DB.XYZ, SOG.Point>
{
  private readonly IScalingServiceToSpeckle _toSpeckleScalingService;
  private readonly IRevitConversionContextStack _contextStack;

  public XyzConversionToPoint(
    IScalingServiceToSpeckle toSpeckleScalingService,
    IRevitConversionContextStack contextStack
  )
  {
    _toSpeckleScalingService = toSpeckleScalingService;
    _contextStack = contextStack;
  }

  public SOG.Point Convert(DB.XYZ target)
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
