using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBEllipseToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<AG.Plane, SOG.Plane> _planeConverter;
  private readonly ITypedConverter<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public DBEllipseToSpeckleConverter(
    ITypedConverter<AG.Plane, SOG.Plane> planeConverter,
    ITypedConverter<ADB.Extents3d, SOG.Box> boxConverter,
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
    SOG.Plane plane = _planeConverter.Convert(new AG.Plane(target.Center, target.MajorAxis, target.MinorAxis));
    SOG.Box bbox = _boxConverter.Convert(target.GeometricExtents);

    // the start and end param corresponds to start and end angle in radians
    SOP.Interval trim = new(target.StartAngle, target.EndAngle);

    SOG.Ellipse ellipse =
      new(plane, target.MajorRadius, target.MinorRadius, _contextStack.Current.SpeckleUnits)
      {
        domain = new(0, Math.PI * 2),
        trimDomain = trim,
        length = target.GetDistanceAtParameter(target.EndParam),
        bbox = bbox
      };

    return ellipse;
  }
}
