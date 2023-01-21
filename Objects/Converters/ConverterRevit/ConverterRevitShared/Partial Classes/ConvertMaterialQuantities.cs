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

      //Get quantities
      double volume = element.GetMaterialVolume(material.Id);
      double area = element.GetMaterialArea(material.Id, false); //To-Do: Do we need Paint-Materials

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
      if (matIDs == null || matIDs.Count() == 0)
        return null;

      var materials = matIDs.Select(material => element.Document.GetElement(material) as DB.Material);
      return MaterialQuantitiesToSpeckle(element, materials, units);
    }
    public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, IEnumerable<DB.Material> materials, string units)
    {
      if (materials == null || materials.Count() == 0) return null;
      List<Objects.Other.MaterialQuantity> quantities = new List<Objects.Other.MaterialQuantity>();

      foreach (var material in materials)
        quantities.Add(MaterialQuantityToSpeckle(element, material, units));

      return quantities;
    }

    #endregion
  }

}
