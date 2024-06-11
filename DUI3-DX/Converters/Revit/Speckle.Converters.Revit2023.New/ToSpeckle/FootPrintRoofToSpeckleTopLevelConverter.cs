using Objects;
using Objects.BuiltElements.Revit.RevitRoof;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;


[NameAndRankValue(nameof(IRevitFootPrintRoof), 0)]
public class FootPrintRoofToSpeckleTopLevelConverter
  : BaseTopLevelConverterToSpeckle<IRevitFootPrintRoof, RevitFootprintRoof>
{
  private readonly ITypedConverter<IRevitLevel, SOBR.RevitLevel> _levelConverter;
  private readonly ITypedConverter<IRevitModelCurveArrArray, SOG.Polycurve[]> _modelCurveArrArrayConverter;
  private readonly IParameterValueExtractor _parameterValueExtractor;
  private readonly IDisplayValueExtractor _displayValueExtractor;
  private readonly IHostedElementConversionToSpeckle _hostedElementConverter;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;
  private readonly IRevitFilterFactory _revitFilterFactory;

  public FootPrintRoofToSpeckleTopLevelConverter(
    ITypedConverter<IRevitLevel, SOBR.RevitLevel> levelConverter,
    ITypedConverter<IRevitModelCurveArrArray, SOG.Polycurve[]> modelCurveArrArrayConverter,
    IParameterValueExtractor parameterValueExtractor,
    IDisplayValueExtractor displayValueExtractor,
    IHostedElementConversionToSpeckle hostedElementConverter,
    IParameterObjectAssigner parameterObjectAssigner, IRevitFilterFactory revitFilterFactory)
  {
    _levelConverter = levelConverter;
    _modelCurveArrArrayConverter = modelCurveArrArrayConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
    _revitFilterFactory = revitFilterFactory;
  }

  public override RevitFootprintRoof Convert(IRevitFootPrintRoof target)
  {
    var baseLevel = _parameterValueExtractor.GetValueAsDocumentObject<IRevitLevel>(
      target,
      RevitBuiltInParameter.ROOF_BASE_LEVEL_PARAM
    );

    // We don't currently validate the success of this TryGet, it is assumed some Roofs don't have a top-level.
    _parameterValueExtractor.TryGetValueAsDocumentObject<IRevitLevel>(
      target,
      RevitBuiltInParameter.ROOF_UPTO_LEVEL_PARAM,
      out var topLevel
    );

    //POC: CNX-9403 can be null if the sides have different slopes.
    //We currently don't validate the success or failure of this TryGet as it's not necessary, but will be once we start the above ticket.
    _parameterValueExtractor.TryGetValueAsDouble(target, RevitBuiltInParameter.ROOF_SLOPE, out var slope);

    RevitFootprintRoof speckleFootprintRoof =
      new()
      {
        level = _levelConverter.Convert(baseLevel),
        cutOffLevel = topLevel is not null ? _levelConverter.Convert(topLevel) : null,
        slope = slope
      };

    // POC: CNX-9396 again with the incorrect assumption that the first profile is the floor and subsequent profiles
    // are voids
    // POC: CNX-9403 in current connector, we are doing serious gymnastics to get the slope of the floor as defined by
    // slope arrow. The way we are doing it relies on dynamic props and only works for Revit <-> Revit
    var profiles = _modelCurveArrArrayConverter.Convert(target.GetProfiles());
    speckleFootprintRoof.outline = profiles.FirstOrDefault();
    speckleFootprintRoof.voids = profiles.Skip(1).ToList<ICurve>();

    var elementType = target.Document.GetElement(target.GetTypeId()).ToType().NotNull();
    speckleFootprintRoof.type = elementType.Name;
    speckleFootprintRoof.family = elementType.FamilyName;

    // POC: we are starting to see logic that is happening in all converters. We should definitely consider some
    // conversion pipeline behavior. Would probably require adding interfaces into objects kit
    _parameterObjectAssigner.AssignParametersToBase(target, speckleFootprintRoof);
    speckleFootprintRoof.displayValue = _displayValueExtractor.GetDisplayValue(target);
    speckleFootprintRoof.elements = _hostedElementConverter
      .ConvertHostedElements(target.GetHostedElementIds(_revitFilterFactory))
      .ToList();

    return speckleFootprintRoof;
  }
}
