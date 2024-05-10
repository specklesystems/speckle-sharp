using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBCircleToSpeckleConverter : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<AG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public DBCircleToSpeckleConverter(
    IRawConversion<AG.Plane, SOG.Plane> planeConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Circle)target);

  public SOG.Circle RawConvert(ADB.Circle target)
  {
    SOG.Plane plane = _planeConverter.RawConvert(target.GetPlane());
    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);
    SOG.Circle circle =
      new(plane, target.Radius, _contextStack.Current.SpeckleUnits) { length = target.Circumference, bbox = bbox };

    return circle;
  }
}
