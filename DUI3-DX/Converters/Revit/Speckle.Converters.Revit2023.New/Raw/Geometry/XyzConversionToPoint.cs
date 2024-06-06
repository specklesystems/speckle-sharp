using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;


public class XyzConversionToPoint : ITypedConverter<IRevitXYZ, SOG.Point>
{
  private readonly IScalingServiceToSpeckle _toSpeckleScalingService;
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;

  public XyzConversionToPoint(
    IScalingServiceToSpeckle toSpeckleScalingService,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack
  )
  {
    _toSpeckleScalingService = toSpeckleScalingService;
    _contextStack = contextStack;
  }

  public SOG.Point Convert(IRevitXYZ target)
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
