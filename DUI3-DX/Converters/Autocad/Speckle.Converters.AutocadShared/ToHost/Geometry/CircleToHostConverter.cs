using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Circle, ADB.Circle>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;
  private readonly ITypedConverter<SOG.Vector, AG.Vector3d> _vectorConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public CircleToHostConverter(
    ITypedConverter<SOG.Point, AG.Point3d> pointConverter,
    ITypedConverter<SOG.Vector, AG.Vector3d> vectorConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Circle)target);

  public ADB.Circle RawConvert(SOG.Circle target)
  {
    AG.Vector3d normal = _vectorConverter.RawConvert(target.plane.normal);
    AG.Point3d origin = _pointConverter.RawConvert(target.plane.origin);
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);

    if (target.radius is null)
    {
      throw new ArgumentNullException(nameof(target), "Cannot convert circle without radius value.");
    }

    var radius = f * (double)target.radius;
    return new(origin, normal, radius);
  }
}
