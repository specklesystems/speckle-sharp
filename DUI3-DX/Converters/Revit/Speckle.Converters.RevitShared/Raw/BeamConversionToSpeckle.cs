using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: There is no validation on this converter to prevent conversion from "not a Revit Beam" to a Speckle Beam.
// This will definitely explode if we tried. Goes back to the `CanConvert` functionality conversation.
// As-is, what we are saying is that it can take "any Family Instance" and turn it into a Speckle.RevitBeam, which is far from correct.
public class BeamConversionToSpeckle : IRawConversion<DB.FamilyInstance, SOBR.RevitBeam>
{
  private readonly IRawConversion<DB.Location, Base> _locationConverter;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public BeamConversionToSpeckle(
    IRawConversion<DB.Location, Base> locationConverter,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _locationConverter = locationConverter;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public SOBR.RevitBeam RawConvert(DB.FamilyInstance target)
  {
    var baseGeometry = _locationConverter.RawConvert(target.Location);
    if (baseGeometry is not ICurve baseCurve)
    {
      throw new SpeckleConversionException(
        $"Beam location conversion did not yield an ICurve, instead it yielded an object of type {baseGeometry.GetType()}"
      );
    }
    var symbol = (DB.FamilySymbol)target.Document.GetElement(target.GetTypeId());

    SOBR.RevitBeam speckleBeam =
      new()
      {
        family = symbol.FamilyName,
        type = target.Document.GetElement(target.GetTypeId()).Name,
        baseLine = baseCurve
      };

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM
    );
    speckleBeam.level = _levelConverter.RawConvert(level);

    speckleBeam.displayValue = _displayValueExtractor.GetDisplayValue(target);

    _parameterObjectAssigner.AssignParametersToBase(target, speckleBeam);

    return speckleBeam;
  }
}
