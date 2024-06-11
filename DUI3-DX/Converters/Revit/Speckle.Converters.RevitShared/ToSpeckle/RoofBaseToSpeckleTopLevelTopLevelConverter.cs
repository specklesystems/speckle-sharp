using Objects.BuiltElements.Revit.RevitRoof;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

[NameAndRankValue(nameof(IRevitRoofBase), 0)]
internal sealed class RoofBaseToSpeckleTopLevelTopLevelConverter
  : BaseTopLevelConverterToSpeckle<IRevitRoofBase, RevitRoof>
{
  private readonly IDisplayValueExtractor _displayValueExtractor;
  private readonly IHostedElementConversionToSpeckle _hostedElementConverter;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;
  private readonly IRevitFilterFactory _revitFilterFactory;

  public RoofBaseToSpeckleTopLevelTopLevelConverter(
    IDisplayValueExtractor displayValueExtractor,
    IHostedElementConversionToSpeckle hostedElementConverter,
    IParameterObjectAssigner parameterObjectAssigner,
    IRevitFilterFactory revitFilterFactory
  )
  {
    _displayValueExtractor = displayValueExtractor;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
    _revitFilterFactory = revitFilterFactory;
  }

  public override RevitRoof Convert(IRevitRoofBase target)
  {
    RevitRoof revitRoof = new();
    var elementType = target.Document.GetElement(target.GetTypeId()).ToType().NotNull();
    revitRoof.type = elementType.Name;
    revitRoof.family = elementType.FamilyName;

    _parameterObjectAssigner.AssignParametersToBase(target, revitRoof);
    revitRoof.displayValue = _displayValueExtractor.GetDisplayValue(target);
    revitRoof.elements = _hostedElementConverter
      .ConvertHostedElements(target.GetHostedElementIds(_revitFilterFactory))
      .ToList();

    return revitRoof;
  }
}
