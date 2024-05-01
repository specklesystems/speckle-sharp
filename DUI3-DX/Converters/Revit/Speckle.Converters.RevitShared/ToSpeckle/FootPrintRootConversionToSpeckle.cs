using Autodesk.Revit.DB;
using Objects;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.RevitRoof;
using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.FootPrintRoof), 0)]
public class FootPrintRootConversionToSpeckle : BaseConversionToSpeckle<DB.FootPrintRoof, RevitFootprintRoof>
{
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly IRawConversion<DB.ModelCurveArrArray, SOG.Polycurve[]> _modelCurveArrArrayConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public FootPrintRootConversionToSpeckle(
    IRawConversion<Level, RevitLevel> levelConverter,
    IRawConversion<ModelCurveArrArray, Polycurve[]> modelCurveArrArrayConverter,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    HostedElementConversionToSpeckle hostedElementConverter,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _levelConverter = levelConverter;
    _modelCurveArrArrayConverter = modelCurveArrArrayConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public override RevitFootprintRoof RawConvert(FootPrintRoof target)
  {
    var baseLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM
    );
    var topLevel = _parameterValueExtractor.GetValueAsDocumentObjectOrNull<DB.Level>(
      target,
      DB.BuiltInParameter.ROOF_UPTO_LEVEL_PARAM
    );

    //NOTE: can be null if the sides have different slopes
    double? slope = _parameterValueExtractor.GetValueAsDoubleOrNull(target, DB.BuiltInParameter.ROOF_SLOPE);

    RevitFootprintRoof speckleFootprintRoof =
      new()
      {
        level = _levelConverter.RawConvert(baseLevel),
        cutOffLevel = topLevel is not null ? _levelConverter.RawConvert(topLevel) : null,
        slope = slope
      };

    // POC: again with the incorrect assumption that the first profile is the floor and subsequent profiles
    // are voids
    // POC: in current connector, we are doing serious gymnastics to get the slope of the floor as defined by
    // slope arrow. The way we are doing it relys on dynamic props and only works for Revit <-> Revit
    var profiles = _modelCurveArrArrayConverter.RawConvert(target.GetProfiles());
    speckleFootprintRoof.outline = profiles.FirstOrDefault();
    speckleFootprintRoof.voids = profiles.Skip(1).ToList<ICurve>();

    var elementType = (ElementType)target.Document.GetElement(target.GetTypeId());
    speckleFootprintRoof.type = elementType.Name;
    speckleFootprintRoof.family = elementType.FamilyName;

    // POC: we are starting to see logic that is happening in all converters. We should definitely consider some
    // conversion pipeline behavior. Would probably require adding interfaces into objects kit
    _parameterObjectAssigner.AssignParametersToBase(target, speckleFootprintRoof);
    speckleFootprintRoof.displayValue = _displayValueExtractor.GetDisplayValue(target);
    _hostedElementConverter.AssignHostedElements(target, speckleFootprintRoof);

    return speckleFootprintRoof;
  }
}
