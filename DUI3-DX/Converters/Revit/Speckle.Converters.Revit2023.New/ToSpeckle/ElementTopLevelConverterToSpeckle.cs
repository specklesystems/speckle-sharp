using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;


// POC: not currently used? clearly some missing pieces
[NameAndRankValue(nameof(IRevitElement), 0)]
public class ElementTopLevelConverterToSpeckle : BaseTopLevelConverterToSpeckle<IRevitElement, SOBR.RevitElement>
{
  private readonly IDisplayValueExtractor _displayValueExtractor;

  public ElementTopLevelConverterToSpeckle(IDisplayValueExtractor displayValueExtractor)
  {
    _displayValueExtractor = displayValueExtractor;
  }
  public override SOBR.RevitElement Convert(IRevitElement target)
  {
    SOBR.RevitElement speckleElement = new();

    var element = target.Document.GetElement(target.GetTypeId());
    var symbol = element.ToFamilySymbol();
    if (symbol is not null)
    {
      speckleElement.family = symbol.FamilyName;
      speckleElement.type = symbol.Name;
    }
    else
    {
      speckleElement.type = target.Name;
    }
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
