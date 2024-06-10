using Objects.Primitive;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class ArcToSpeckleConverter : ITypedConverter<IRevitArc, SOG.Arc>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<IRevitPlane, SOG.Plane> _planeConverter;
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly IRevitPlaneUtils _revitPlaneUtils;

  public ArcToSpeckleConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<IRevitPlane, SOG.Plane> planeConverter,
    IScalingServiceToSpeckle scalingService, IRevitPlaneUtils revitPlaneUtils)
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
    _revitPlaneUtils = revitPlaneUtils;
  }

  public SOG.Arc Convert(IRevitArc target)
  {
    // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
    var arcPlane = _revitPlaneUtils.CreateByOriginAndBasis(target.Center, target.XDirection, target.YDirection);
    IRevitXYZ center = target.Center;

    IRevitXYZ dir0 = (target.GetEndPoint(0).Subtract(center)).Normalize();
    IRevitXYZ dir1 = (target.GetEndPoint(1).Subtract(center)).Normalize();

    IRevitXYZ start = target.Evaluate(0, true);
    IRevitXYZ end = target.Evaluate(1, true);
    IRevitXYZ mid = target.Evaluate(0.5, true);

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
