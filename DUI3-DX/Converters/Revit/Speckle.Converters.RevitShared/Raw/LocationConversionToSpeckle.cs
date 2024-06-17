using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Revit.Interfaces;

#pragma warning disable IDE0130
namespace Speckle.Converters.RevitShared;

#pragma warning restore IDE0130

public class LocationConversionToSpeckle : ITypedConverter<IRevitLocation, Base>
{
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter;
  private readonly ITypedConverter<IRevitXYZ, Objects.Geometry.Point> _xyzConverter;

  // POC: review IRawConversion<TIn> which always returns a Base, this is ToSpeckle, so... this breaks
  // the meaning of IRawConversion, it could be IToSpeckleRawConversion
  // also a factory type
  public LocationConversionToSpeckle(
    ITypedConverter<IRevitCurve, ICurve> curveConverter,
    ITypedConverter<IRevitXYZ, Objects.Geometry.Point> xyzConverter
  )
  {
    _curveConverter = curveConverter;
    _xyzConverter = xyzConverter;
  }

  public Base Convert(IRevitLocation target)
  {
    var curve = target.ToLocationCurve();
    if (curve is not null)
    {
      return (_curveConverter.Convert(curve.Curve) as Base).NotNull(); // POC: ICurve and Base are not related but we know they must be, had to soft cast and then !.
    }
    var point = target.ToLocationPoint();
    if (point is not null)
    {
      return _xyzConverter.Convert(point.Point);
    }
    throw new SpeckleConversionException($"Unexpected location type {target.GetType()}");
  }
}
