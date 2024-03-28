using Autodesk.Revit.DB;
using Objects;
using Objects.BuiltElements.Revit;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class BeamConversionToSpeckle : IRawConversion<DB.FamilyInstance, SOBR.RevitBeam>
{
  private readonly IRawConversion<DB.Location> _locationConverter;
  private readonly IRawConversion<DB.Level, RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public BeamConversionToSpeckle(
    IRawConversion<Location> locationConverter,
    IRawConversion<Level, RevitLevel> levelConverter,
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

  public RevitBeam RawConvert(DB.FamilyInstance target)
  {
    var baseGeometry = _locationConverter.ConvertToBase(target.Location);
    if (baseGeometry is not ICurve baseCurve)
    {
      throw new SpeckleConversionException(
        $"Beam location conversion did not yield an ICurve, instead it yielded an object of type {baseGeometry.GetType()}"
      );
    }
    var symbol = (FamilySymbol)target.Document.GetElement(target.GetTypeId());

    RevitBeam speckleBeam =
      new()
      {
        family = symbol.FamilyName,
        type = target.Document.GetElement(target.GetTypeId()).Name,
        baseLine = baseCurve
      };

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM
    );
    speckleBeam.level = _levelConverter.RawConvert(level);

    speckleBeam.displayValue = _displayValueExtractor.GetDisplayValue(target);

    _parameterObjectAssigner.AssignParametersToBase(target, speckleBeam);

    return speckleBeam;
  }
}
