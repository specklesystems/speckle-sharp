using Objects;
using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class CircleToSpeckleConverter : IRawConversion<DB.Arc, ICurve>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.Plane, SOG.Plane> _planeConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public CircleToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    IRawConversion<DB.Plane, SOG.Plane> planeConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _contextStack = contextStack;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
  }

  public ICurve RawConvert(DB.Arc arc)
  {
    // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
    var units = _contextStack.Current.SpeckleUnits;
    var arcPlane = DB.Plane.CreateByNormalAndOrigin(arc.Normal, arc.Center);
    var c = new SOG.Circle(_planeConverter.RawConvert(arcPlane), _scalingService.ScaleLength(arc.Radius), units);
    c.length = _scalingService.ScaleLength(arc.Length);

    return c;
  }
}
