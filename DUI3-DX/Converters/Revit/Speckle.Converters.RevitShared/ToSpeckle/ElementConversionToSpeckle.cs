using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Objects.BuiltElements.Revit;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Element), 0)]
public class ElementConversionToSpeckle : BaseConversionToSpeckle<DB.Element, RevitElement>
{
  private readonly DisplayValueExtractor _displayValueExtractor;

  public ElementConversionToSpeckle(DisplayValueExtractor displayValueExtractor)
  {
    _displayValueExtractor = displayValueExtractor;
  }

  public override RevitElement RawConvert(Element target)
  {
    RevitElement speckleElement = new();

    //var symbol = target.Document.GetElement(target.GetTypeId()) as FamilySymbol;
    //if (symbol != null)
    //{
    //  speckleElement.family = symbol.FamilyName;
    //  speckleElement.type = symbol.Name;
    //}
    //else
    //{
    //  speckleElement.type = target.Name;
    //}
    speckleElement.type = target.Name;

    //var baseGeometry = LocationToSpeckle(target);
    //if (baseGeometry is Geometry.Point point)
    //{
    //  speckleElement["basePoint"] = point;
    //}
    //else if (baseGeometry is Geometry.Line line)
    //{
    //  speckleElement["baseLine"] = line;
    //}

    speckleElement.category = target.Category.Name;

    speckleElement.displayValue = _displayValueExtractor.GetDisplayValue(target);

    //GetHostedElements(speckleElement, target, out notes);

    //var displayValue = GetElementDisplayValue(target);

    //if (!displayValue.Any())
    //{
    //  notes.Add(
    //    "Element does not have visible geometry. It will be sent to Speckle but won't be visible in the viewer."
    //  );
    //}
    //else
    //{
    //  speckleElement.displayValue = displayValue;
    //}

    //GetAllRevitParamsAndIds(speckleElement, target);

    return speckleElement;
  }
}
