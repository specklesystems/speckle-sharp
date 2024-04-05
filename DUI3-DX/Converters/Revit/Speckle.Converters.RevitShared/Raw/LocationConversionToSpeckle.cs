using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LocationConversionToSpeckle : IRawConversion<DB.Location, Base>
{
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzConverter;

  // POC: review IRawConversion<TIn> which always returns a Base, this is ToSpeckle, so... this breaks
  // the meaning of IRawConversion, it could be IToSpeckleRawConversion
  // also a factory type
  public LocationConversionToSpeckle(
    IRawConversion<DB.Curve, ICurve> curveConverter,
    IRawConversion<DB.XYZ, SOG.Point> xyzConverter
  )
  {
    _curveConverter = curveConverter;
    _xyzConverter = xyzConverter;
  }

  public Base RawConvert(DB.Location target)
  {
    return target switch
    {
      DB.LocationCurve curve => (_curveConverter.RawConvert(curve.Curve) as Base)!, // POC: ICurve and Base are not related but we know they must be, had to soft cast and then !.
      DB.LocationPoint point => _xyzConverter.RawConvert(point.Point),
      _ => throw new SpeckleConversionException($"Unexpected location type {target.GetType()}")
    };
  }
}
