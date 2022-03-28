using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using System;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public RevitElement RevitElementToSpeckle(Element revitElement)
    {
      var symbol = revitElement.Document.GetElement(revitElement.GetTypeId()) as FamilySymbol;

      RevitElement speckleElement = new RevitElement();
      if (symbol != null)
      {
        speckleElement.family = symbol.FamilyName;
        speckleElement.type = symbol.Name;
      }
      else
      {
        speckleElement.type = revitElement.Name;
      }

      speckleElement.category = revitElement.Category.Name;
      speckleElement.displayValue = GetElementDisplayMesh(revitElement, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      //Only send elements that have a mesh, if not we should probably support them properly via direct conversions
      if (speckleElement.displayValue == null || speckleElement.displayValue.Count == 0)
        throw new Exception($"Skipped not supported type: {revitElement.GetType()}{GetElemInfo(revitElement)}");

      GetAllRevitParamsAndIds(speckleElement, revitElement);

      return speckleElement;
    }
  }
}
