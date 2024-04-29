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

    if (polycurve.polyType == AutocadPolyType.Light)
    {
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
