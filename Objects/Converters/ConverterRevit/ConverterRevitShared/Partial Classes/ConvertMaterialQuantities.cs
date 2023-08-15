using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    #region MaterialQuantity
    /// <summary>
    /// Gets the quantitiy of a material in one element
    /// </summary>
    /// <param name="element"></param>
    /// <param name="material"></param>
    /// <returns></returns>
    public Objects.Other.MaterialQuantity MaterialQuantityToSpeckle(DB.Element element, DB.Material material, string units)
    {
      if (material == null || element == null) 
        return null;

      // To-Do: These methods from the Revit API appear to have bugs.
      double volume = element.GetMaterialVolume(material.Id);
      double area = element.GetMaterialArea(material.Id, false);

      // Convert revit interal units to speckle commit units
      double factor = ScaleToSpeckle(1);
      volume *= factor * factor * factor;
      area *= factor * factor;

      var speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
      var materialQuantity = new Objects.Other.MaterialQuantity(speckleMaterial, volume, area, units);

      if (LocationToSpeckle(element) is ICurve curve)
        materialQuantity["length"] = curve.length;
      return materialQuantity;
    }

    #endregion

    #region MaterialQuantities
    public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, string units)
    {
      
      var matIDs = element?.GetMaterialIds(false); 
      // Does not return the correct materials for some categories
      // Need to take different approach for MEP-Elements
      if (matIDs == null || !matIDs.Any() &&  element is MEPCurve)
      {
        DB.Material mepMaterial = ConverterRevit.GetMEPSystemRevitMaterial(element);
        if (mepMaterial != null) matIDs.Add(mepMaterial.Id);
      }

      if (matIDs == null || !matIDs.Any())
        return null;

      var materials = matIDs.Select(material => element.Document.GetElement(material) as DB.Material);
      return MaterialQuantitiesToSpeckle(element, materials, units);
    }
    public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, IEnumerable<DB.Material> materials, string units)
    {
      if (materials == null || !materials.Any()) return null;
      List<Objects.Other.MaterialQuantity> quantities = new List<Objects.Other.MaterialQuantity>();

      foreach (var material in materials)
        quantities.Add(MaterialQuantityToSpeckle(element, material, units));

      return quantities;
    }

    #endregion
    
  }

}
