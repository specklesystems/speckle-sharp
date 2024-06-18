using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared;

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

    var symbol = target.Document.GetElement(target.GetTypeId()).NotNull().ToFamilySymbol().NotNull();

    SOBR.RevitBeam speckleBeam =
      new()
      {
        family = symbol.FamilyName,
        type = target.Document.GetElement(target.GetTypeId()).NotNull().Name,
        baseLine = baseCurve
      };

    var level = _parameterValueExtractor.GetValueAsRevitLevel(
      target,
      RevitBuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM
    );
    speckleBeam.level = _levelConverter.Convert(level);

    speckleBeam.displayValue = _displayValueExtractor.GetDisplayValue(target);

    _parameterObjectAssigner.AssignParametersToBase(target, speckleBeam);

    return speckleBeam;
  }
}
