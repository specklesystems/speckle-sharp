using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class CircleToSpeckleConverter : ITypedConverter<IRevitArc, SOG.Circle>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitPlane, SOG.Plane> _planeConverter;
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly IRevitPlaneUtils _revitPlaneUtils;

  public CircleToSpeckleConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitPlane, SOG.Plane> planeConverter,
    IScalingServiceToSpeckle scalingService, IRevitPlaneUtils revitPlaneUtils)
  {
    _contextStack = contextStack;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
    _revitPlaneUtils = revitPlaneUtils;
  }

  public SOG.Circle Convert(IRevitArc target)
  {
    // POC: should we check for arc of 360 and throw? Original CircleToSpeckle did not do this.

    // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
    var arcPlane = _revitPlaneUtils.CreateByNormalAndOrigin(target.Normal, target.Center);
    var c = new SOG.Circle()
    {
      plane = _planeConverter.Convert(arcPlane),
      radius = _scalingService.ScaleLength(target.Radius),
      units = _contextStack.Current.SpeckleUnits,
      length = _scalingService.ScaleLength(target.Length)
    };

    return c;
  }
}
