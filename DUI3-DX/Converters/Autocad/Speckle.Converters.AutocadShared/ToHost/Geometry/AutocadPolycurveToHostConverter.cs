using Objects.Geometry.Autocad;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad2023.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Autocad.AutocadPolycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class AutocadPolycurveToHostConverter : ISpeckleObjectToHostConversion
{
  private readonly IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline> _polylineConverter;
  private readonly IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline2d> _polyline2dConverter;
  private readonly IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline3d> _polyline3dConverter;

  public AutocadPolycurveToHostConverter(
    IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline> polylineConverter,
    IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline2d> polyline2dConverter,
    IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline3d> polyline3dConverter
  )
  {
    _polylineConverter = polylineConverter;
    _polyline2dConverter = polyline2dConverter;
    _polyline3dConverter = polyline3dConverter;
  }

  public object Convert(Base target)
  {
    SOG.Autocad.AutocadPolycurve polycurve = (SOG.Autocad.AutocadPolycurve)target;

    if (polycurve.value.Count % 3 != 0)
    {
      throw new SpeckleConversionException(
        $"{nameof(SOG.Autocad.AutocadPolycurve)}.{nameof(polycurve.value)} list is malformed: expected length to be multiple of 3"
      );
    }

    if (polycurve.polyType == AutocadPolyType.Light)
    {
      // POC: Anti-POC because there is nothing we can do about ADB.Polyline. This object can be 3d as planar and we can convert to speckle with 3d vertices
      // but there is no API that you can create ADB.Polyline from this 3d points. It only accepts 2d... 😠
      return _polylineConverter.RawConvert(polycurve);
    }
    else if (
      polycurve.polyType == AutocadPolyType.Simple2d
      || polycurve.polyType == AutocadPolyType.FitCurve2d
      || polycurve.polyType == AutocadPolyType.CubicSpline2d
      || polycurve.polyType == AutocadPolyType.QuadSpline2d
    )
    {
      return _polyline2dConverter.RawConvert(polycurve);
    }
    else if (
      polycurve.polyType == AutocadPolyType.Simple3d
      || polycurve.polyType == AutocadPolyType.CubicSpline3d
      || polycurve.polyType == AutocadPolyType.QuadSpline3d
    )
    {
      return _polyline3dConverter.RawConvert(polycurve);
    }
    else
    {
      throw new SpeckleConversionException("Unknown poly type for AutocadPolycurve");
    }
  }
}
