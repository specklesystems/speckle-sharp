using System.Collections.Generic;

using Autodesk.Revit.DB;

using Speckle.Core.Models;

using Objects.BuiltElements.Revit;
using RevitElementType = Objects.BuiltElements.Revit.RevitElementType;
using System.Linq;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public RevitElement RevitElementToSpeckle(Element revitElement, out List<string> notes, RevitElement speckleElement = null)
    {
      notes = new List<string>();
      var symbol = revitElement.Document.GetElement(revitElement.GetTypeId()) as FamilySymbol;

      speckleElement ??= new RevitElement();
      if (symbol != null)
      {
        speckleElement.family = symbol.FamilyName;
        speckleElement.type = symbol.Name;
      }
      else
      {
        speckleElement.type = revitElement.Name;
      }

      var baseGeometry = LocationToSpeckle(revitElement);
      if (baseGeometry is Geometry.Point point)
        speckleElement["basePoint"] = point;
      else if (baseGeometry is Geometry.Line line)
        speckleElement["baseLine"] = line;

      speckleElement.category = revitElement.Category.Name;

      GetHostedElements(speckleElement, revitElement, out notes);

      var displayValue = GetElementDisplayValue(revitElement);

      if (!displayValue.Any())
        notes.Add(
          "Element does not have visible geometry. It will be sent to Speckle but won't be visible in the viewer."
        );
      else
        speckleElement.displayValue = displayValue;

      GetAllRevitParamsAndIds(speckleElement, revitElement);

      return speckleElement;
    }

    public RevitElementType ElementTypeToSpeckle(ElementType revitType)
    {
      var type = revitType.Name;
      var family = revitType.FamilyName;
      var category = revitType.Category.Name;
      RevitElementType speckleType = null;

      switch (revitType)
      {
        case FamilySymbol o:
          var symbolType = new RevitSymbolElementType()
          {
            type = type,
            family = family,
            category = category
          };
          symbolType.placementType = o.Family?.FamilyPlacementType.ToString();
          speckleType = symbolType;
          break;
        case MEPCurveType o:
          var mepType = new RevitMepElementType()
          {
            type = type,
            family = family,
            category = category
          };
          mepType.shape = o.Shape.ToString();
          speckleType = mepType;
          break;
        default:
          speckleType = new RevitElementType()
          {
            type = type,
            family = family,
            category = category
          };
          break;
      }

      GetAllRevitParamsAndIds(speckleType, revitType);

      return speckleType;
    }
  }
}
