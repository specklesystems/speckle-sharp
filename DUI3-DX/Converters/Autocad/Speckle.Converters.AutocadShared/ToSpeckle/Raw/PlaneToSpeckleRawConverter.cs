using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class PlaneToSpeckleRawConverter : IHostObjectToSpeckleConversion, ITypedConverter<AG.Plane, SOG.Plane>
{
  private readonly ITypedConverter<AG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly ITypedConverter<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public PlaneToSpeckleRawConverter(
    ITypedConverter<AG.Vector3d, SOG.Vector> vectorConverter,
    ITypedConverter<AG.Point3d, SOG.Point> pointConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _vectorConverter = vectorConverter;
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((AG.Plane)target);

  public SOG.Plane RawConvert(AG.Plane target) =>
    new(
      _pointConverter.RawConvert(target.PointOnPlane),
      _vectorConverter.RawConvert(target.Normal),
      _vectorConverter.RawConvert(target.GetCoordinateSystem().Xaxis),
      _vectorConverter.RawConvert(target.GetCoordinateSystem().Yaxis),
      _contextStack.Current.SpeckleUnits
    );
}
