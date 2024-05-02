using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit.RevitRoof;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.RoofBase), 0)]
internal sealed class RoofBaseConversionToSpeckle : BaseConversionToSpeckle<DB.RoofBase, RevitRoof>
{
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public RoofBaseConversionToSpeckle(
    DisplayValueExtractor displayValueExtractor,
    HostedElementConversionToSpeckle hostedElementConverter,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _displayValueExtractor = displayValueExtractor;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public override RevitRoof RawConvert(RoofBase target)
  {
    RevitRoof revitRoof = new();
    var elementType = (ElementType)target.Document.GetElement(target.GetTypeId());
    revitRoof.type = elementType.Name;
    revitRoof.family = elementType.FamilyName;

    _parameterObjectAssigner.AssignParametersToBase(target, revitRoof);
    revitRoof.displayValue = _displayValueExtractor.GetDisplayValue(target);
    revitRoof.elements = _hostedElementConverter.ConvertHostedElements(target.GetHostedElementIds()).ToList();

    return revitRoof;
  }
}
