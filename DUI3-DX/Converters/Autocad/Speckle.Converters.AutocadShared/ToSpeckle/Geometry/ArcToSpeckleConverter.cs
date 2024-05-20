using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBArcToSpeckleConverter : IHostObjectToSpeckleConversion, ITypedConverter<ADB.Arc, SOG.Arc>
{
  private readonly ITypedConverter<AG.Point3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<AG.Plane, SOG.Plane> _planeConverter;
  private readonly ITypedConverter<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public DBArcToSpeckleConverter(
    ITypedConverter<AG.Point3d, SOG.Point> pointConverter,
    ITypedConverter<AG.Plane, SOG.Plane> planeConverter,
    ITypedConverter<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Arc)target);

  public SOG.Arc RawConvert(ADB.Arc target)
  {
    SOG.Plane plane = _planeConverter.RawConvert(target.GetPlane());
    SOG.Point start = _pointConverter.RawConvert(target.StartPoint);
    SOG.Point end = _pointConverter.RawConvert(target.EndPoint);
    SOG.Point mid = _pointConverter.RawConvert(target.GetPointAtDist(target.Length / 2.0));
    SOP.Interval domain = new(target.StartParam, target.EndParam);
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Arc arc =
      new(
        plane,
        target.Radius,
        target.StartAngle,
        target.EndAngle,
        target.TotalAngle,
        _contextStack.Current.SpeckleUnits
      )
      {
        startPoint = start,
        endPoint = end,
        midPoint = mid,
        domain = domain,
        length = target.Length,
        bbox = bbox
      };

    return arc;
  }
}
