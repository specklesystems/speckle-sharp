using Autodesk.Revit.DB;
using Objects;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.RevitRoof;
using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.FootPrintRoof), 0)]
public class FootPrintRootToSpeckleConverter : BaseConversionToSpeckle<DB.FootPrintRoof, RevitFootprintRoof>
{
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly IRawConversion<DB.ModelCurveArrArray, SOG.Polycurve[]> _modelCurveArrArrayConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public FootPrintRootToSpeckleConverter(
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

    // We don't currently validate the success of this TryGet, it is assumed some Roofs don't have a top-level.
    _parameterValueExtractor.TryGetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.ROOF_UPTO_LEVEL_PARAM,
      out var topLevel
    );

    //POC: CNX-9403 can be null if the sides have different slopes.
    //We currently don't validate the success or failure of this TryGet as it's not necessary, but will be once we start the above ticket.
    _parameterValueExtractor.TryGetValueAsDouble(target, DB.BuiltInParameter.ROOF_SLOPE, out var slope);

    RevitFootprintRoof speckleFootprintRoof =
      new()
      {
        level = _levelConverter.RawConvert(baseLevel),
        cutOffLevel = topLevel is not null ? _levelConverter.RawConvert(topLevel) : null,
        slope = slope
      };

    // POC: CNX-9396 again with the incorrect assumption that the first profile is the floor and subsequent profiles
    // are voids
    // POC: CNX-9403 in current connector, we are doing serious gymnastics to get the slope of the floor as defined by
    // slope arrow. The way we are doing it relies on dynamic props and only works for Revit <-> Revit
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
    speckleFootprintRoof.elements = _hostedElementConverter
      .ConvertHostedElements(target.GetHostedElementIds())
      .ToList();

    return speckleFootprintRoof;
  }
}
