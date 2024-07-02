using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LocationConversionToSpeckle : ITypedConverter<DB.Location, Base>
{
  private readonly ITypedConverter<DB.Curve, ICurve> _curveConverter;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzConverter;

  // POC: review IRawConversion<TIn> which always returns a Base, this is ToSpeckle, so... this breaks
  // the meaning of IRawConversion, it could be IToSpeckleRawConversion
  // also a factory type
  public LocationConversionToSpeckle(
    ITypedConverter<DB.Curve, ICurve> curveConverter,
    ITypedConverter<DB.XYZ, SOG.Point> xyzConverter
  )
  {
    _curveConverter = curveConverter;
    _xyzConverter = xyzConverter;
  }

  public Base Convert(DB.Location target)
  {
    return target switch
    {
      DB.LocationCurve curve => (_curveConverter.Convert(curve.Curve) as Base)!, // POC: ICurve and Base are not related but we know they must be, had to soft cast and then !.
      DB.LocationPoint point => _xyzConverter.Convert(point.Point),
      _ => throw new SpeckleConversionException($"Unexpected location type {target.GetType()}")
    };
  }
}
