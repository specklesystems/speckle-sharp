using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using System;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public RevitElement RevitElementToSpeckle(Element revitElement, out List<string> notes)
    {
      notes = new List<string>();
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
      {
        notes.Add("Not sending elements without display meshes");
        return null;
      }

      GetAllRevitParamsAndIds(speckleElement, revitElement);

      return speckleElement;
    }
  }
}
