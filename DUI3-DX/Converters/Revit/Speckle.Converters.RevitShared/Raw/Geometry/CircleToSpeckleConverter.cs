using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class CircleToSpeckleConverter : ITypedConverter<DB.Arc, SOG.Circle>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.Plane, SOG.Plane> _planeConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public CircleToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.Plane, SOG.Plane> planeConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _contextStack = contextStack;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
  }

  public SOG.Circle Convert(DB.Arc target)
  {
    // POC: should we check for arc of 360 and throw? Original CircleToSpeckle did not do this.

    // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
    var arcPlane = DB.Plane.CreateByNormalAndOrigin(target.Normal, target.Center);
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
