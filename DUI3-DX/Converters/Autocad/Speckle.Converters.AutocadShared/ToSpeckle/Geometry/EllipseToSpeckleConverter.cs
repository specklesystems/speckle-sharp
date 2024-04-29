using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBEllipseToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public DBEllipseToSpeckleConverter(
    IRawConversion<AG.Plane, SOG.Plane> planeConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Ellipse)target);

  public SOG.Ellipse RawConvert(ADB.Ellipse target)
  {
    SOG.Plane plane = _planeConverter.RawConvert(new AG.Plane(target.Center, target.MajorAxis, target.MinorAxis));
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Ellipse ellipse =
      new(plane, target.MajorRadius, target.MinorRadius, _contextStack.Current.SpeckleUnits)
      {
        domain = new SOP.Interval(target.StartParam, target.EndParam),
        length = target.GetDistanceAtParameter(target.EndParam),
        bbox = bbox
      };

    return ellipse;
  }
}
