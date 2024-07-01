using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class ArcToSpeckleConverter : ITypedConverter<DB.Arc, SOG.Arc>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<DB.Plane, SOG.Plane> _planeConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public ArcToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<DB.Plane, SOG.Plane> planeConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
  }

  public SOG.Arc Convert(DB.Arc target)
  {
    // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
    var arcPlane = DB.Plane.CreateByOriginAndBasis(target.Center, target.XDirection, target.YDirection);
    DB.XYZ center = target.Center;

    DB.XYZ dir0 = (target.GetEndPoint(0) .Subtract( center)).Normalize();
    DB.XYZ dir1 = (target.GetEndPoint(1) .Subtract( center)).Normalize();

    DB.XYZ start = target.Evaluate(0, true);
    DB.XYZ end = target.Evaluate(1, true);
    DB.XYZ mid = target.Evaluate(0.5, true);

    double startAngle = target.XDirection.AngleOnPlaneTo(dir0, target.Normal);
    double endAngle = target.XDirection.AngleOnPlaneTo(dir1, target.Normal);

    return new SOG.Arc()
    {
      plane = _planeConverter.Convert(arcPlane),
      radius = _scalingService.ScaleLength(target.Radius),
      startAngle = startAngle,
      endAngle = endAngle,
      angleRadians = endAngle - startAngle,
      units = _contextStack.Current.SpeckleUnits,
      endPoint = _xyzToPointConverter.Convert(end),
      startPoint = _xyzToPointConverter.Convert(start),
      midPoint = _xyzToPointConverter.Convert(mid),
      length = _scalingService.ScaleLength(target.Length),
      domain = new Interval(target.GetEndParameter(0), target.GetEndParameter(1))
    };
  }
}
