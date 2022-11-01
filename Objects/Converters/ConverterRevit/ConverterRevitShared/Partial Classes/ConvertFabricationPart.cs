using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Convert fabrication parts to speckle elements
    /// </summary>
    /// <param name="revitElement">Fabrication part in Revit</param>
    /// <param name="notes">Conversion notes for the exceptional cases</param>
    /// <returns>RevitElement which represents converted fabrication part as speckle object</returns>
    public RevitElement FabricationPartToSpeckle(FabricationPart revitElement, out List<string> notes)
    {
      notes = new List<string>();

      RevitElement speckleElement = new RevitElement();

      speckleElement.type = revitElement.Name;

      speckleElement.category = revitElement.Category.Name;

      speckleElement.displayValue = GetFabricationMeshes(revitElement);

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