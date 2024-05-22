using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad2023.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Autocad.AutocadPolycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class AutocadPolycurveToHostConverter : IToHostTopLevelConverter
{
  private readonly ITypedConverter<SOG.Autocad.AutocadPolycurve, ADB.Polyline> _polylineConverter;
  private readonly ITypedConverter<SOG.Autocad.AutocadPolycurve, ADB.Polyline2d> _polyline2dConverter;
  private readonly ITypedConverter<SOG.Autocad.AutocadPolycurve, ADB.Polyline3d> _polyline3dConverter;

  public AutocadPolycurveToHostConverter(
    ITypedConverter<SOG.Autocad.AutocadPolycurve, ADB.Polyline> polylineConverter,
    ITypedConverter<SOG.Autocad.AutocadPolycurve, ADB.Polyline2d> polyline2dConverter,
    ITypedConverter<SOG.Autocad.AutocadPolycurve, ADB.Polyline3d> polyline3dConverter
  )
  {
    _polylineConverter = polylineConverter;
    _polyline2dConverter = polyline2dConverter;
    _polyline3dConverter = polyline3dConverter;
  }

  public object Convert(Base target)
  {
    SOG.Autocad.AutocadPolycurve polycurve = (SOG.Autocad.AutocadPolycurve)target;

    switch (polycurve.polyType)
    {
      case SOG.Autocad.AutocadPolyType.Light:
        return _polylineConverter.Convert(polycurve);

      case SOG.Autocad.AutocadPolyType.Simple2d:
      case SOG.Autocad.AutocadPolyType.FitCurve2d:
      case SOG.Autocad.AutocadPolyType.CubicSpline2d:
      case SOG.Autocad.AutocadPolyType.QuadSpline2d:
        return _polyline2dConverter.Convert(polycurve);

      case SOG.Autocad.AutocadPolyType.Simple3d:
      case SOG.Autocad.AutocadPolyType.CubicSpline3d:
      case SOG.Autocad.AutocadPolyType.QuadSpline3d:
        return _polyline3dConverter.Convert(polycurve);

      default:
        throw new SpeckleConversionException("Unknown poly type for AutocadPolycurve");
    }
  }
}
