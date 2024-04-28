using Objects;
using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class ArcToSpeckleConverter : IRawConversion<DB.Arc, ICurve>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly IRawConversion<DB.Plane, SOG.Plane> _planeConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public ArcToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter,
    IRawConversion<DB.Plane, SOG.Plane> planeConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
  }

  public ICurve RawConvert(DB.Arc arc)
  {
    var units = _contextStack.Current.SpeckleUnits;
    // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
    var arcPlane = DB.Plane.CreateByOriginAndBasis(arc.Center, arc.XDirection, arc.YDirection);
    DB.XYZ center = arc.Center;

    DB.XYZ dir0 = (arc.GetEndPoint(0) - center).Normalize();
    DB.XYZ dir1 = (arc.GetEndPoint(1) - center).Normalize();

    DB.XYZ start = arc.Evaluate(0, true);
    DB.XYZ end = arc.Evaluate(1, true);
    DB.XYZ mid = arc.Evaluate(0.5, true);

    double startAngle = arc.XDirection.AngleOnPlaneTo(dir0, arc.Normal);
    double endAngle = arc.XDirection.AngleOnPlaneTo(dir1, arc.Normal);

    var a = new SOG.Arc(
      _planeConverter.RawConvert(arcPlane),
      _scalingService.ScaleLength(arc.Radius),
      startAngle,
      endAngle,
      endAngle - startAngle,
      units
    );
    a.endPoint = _xyzToPointConverter.RawConvert(end);
    a.startPoint = _xyzToPointConverter.RawConvert(start);
    a.midPoint = _xyzToPointConverter.RawConvert(mid);
    a.length = _scalingService.ScaleLength(arc.Length);
    a.domain = new Interval(arc.GetEndParameter(0), arc.GetEndParameter(1));

    return a;
  }
}
