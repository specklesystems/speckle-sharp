using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Revit.Interfaces;

#pragma warning disable IDE0130
namespace Speckle.Converters.Revit2023;
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
    return target switch
    {
      IRevitLocationCurve curve => (_curveConverter.Convert(curve.Curve) as Base).NotNull(), // POC: ICurve and Base are not related but we know they must be, had to soft cast and then !.
      IRevitLocationPoint point => _xyzConverter.Convert(point.Point),
      _ => throw new SpeckleConversionException($"Unexpected location type {target.GetType()}")
    };
  }
}


// POC: There is no validation on this converter to prevent conversion from "not a Revit Beam" to a Speckle Beam.
// This will definitely explode if we tried. Goes back to the `CanConvert` functionality conversation.
// As-is, what we are saying is that it can take "any Family Instance" and turn it into a Speckle.RevitBeam, which is far from correct.
// CNX-9312
public class BeamConversionToSpeckle : ITypedConverter<IRevitFamilyInstance, SOBR.RevitBeam>
{
  private readonly ITypedConverter<IRevitLocation, Base> _locationConverter;
  private readonly ITypedConverter<IRevitLevel, SOBR.RevitLevel> _levelConverter;
  private readonly IParameterValueExtractor _parameterValueExtractor;
  private readonly IDisplayValueExtractor _displayValueExtractor;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;

  public BeamConversionToSpeckle(
    ITypedConverter<IRevitLocation, Base> locationConverter,
    ITypedConverter<IRevitLevel, SOBR.RevitLevel> levelConverter,
    IParameterValueExtractor parameterValueExtractor,
    IDisplayValueExtractor displayValueExtractor,
    IParameterObjectAssigner parameterObjectAssigner
  )
  {
    _locationConverter = locationConverter;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public SOBR.RevitBeam Convert(IRevitFamilyInstance target)
  {
    var baseGeometry = _locationConverter.Convert(target.Location);
    if (baseGeometry is not ICurve baseCurve)
    {
      throw new SpeckleConversionException(
        $"Beam location conversion did not yield an ICurve, instead it yielded an object of type {baseGeometry.GetType()}"
      );
    }

    var symbol = target.Document.GetElement(target.GetTypeId()).ToFamilySymbol().NotNull();

    SOBR.RevitBeam speckleBeam =
      new()
      {
        family = symbol.FamilyName,
        type = target.Document.GetElement(target.GetTypeId()).Name,
        baseLine = baseCurve
      };

    var level = _parameterValueExtractor.GetValueAsDocumentObject<IRevitLevel>(
      target,
      RevitBuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM
    );
    speckleBeam.level = _levelConverter.Convert(level);

    speckleBeam.displayValue = _displayValueExtractor.GetDisplayValue(target);

    _parameterObjectAssigner.AssignParametersToBase(target, speckleBeam);

    return speckleBeam;
  }
}
