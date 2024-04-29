using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class CircularArc3dToSpeckleConverter : IRawConversion<AG.CircularArc3d, SOG.Arc>
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<AG.Plane, SOG.Plane> _planeConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public CircularArc3dToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<AG.Plane, SOG.Plane> planeConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _planeConverter = planeConverter;
    _contextStack = contextStack;
  }

  public SOG.Arc RawConvert(AG.CircularArc3d target)
  {
    SOG.Plane plane = _planeConverter.RawConvert(target.GetPlane());
    SOG.Point start = _pointConverter.RawConvert(target.StartPoint);
    SOG.Point end = _pointConverter.RawConvert(target.EndPoint);
    SOG.Point mid = _pointConverter.RawConvert(target.EvaluatePoint(0.5)); // POC: testing, unsure
    SOP.Interval domain = new(target.GetInterval().LowerBound, target.GetInterval().UpperBound);

    SOG.Arc arc =
      new(
        plane,
        target.Radius,
        target.StartAngle,
        target.EndAngle,
        target.EndAngle - target.StartAngle, // POC: testing, unsure
        _contextStack.Current.SpeckleUnits
      )
      {
        startPoint = start,
        endPoint = end,
        midPoint = mid,
        domain = domain,
        length = target.GetLength(0, 1, 0.000)
      };

    return arc;
  }
}
