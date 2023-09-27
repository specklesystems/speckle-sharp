using Autodesk.Revit.DB;
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
      double volume = 0;
      double area=0;
      if(element is HostObject)
      {
        volume += element.GetMaterialVolume(material.Id);
        area += element.GetMaterialArea(material.Id, false);
      }
      else if(element is DB.Plumbing.Pipe || element is DB.Plumbing.FlexPipe)
      {
        // Area and Volume are computed based on geometry elements
        GetGeometry(element, out List<DB.Mesh> meshes, out List<DB.Solid> solids);
        volume += solids.Sum(s => s.Volume);
        area += solids.Select(s => s.Faces.Cast<DB.Face>().Select(face => face.Area).Max()).Sum();
      }
      else
      {
        // Area and Volume are computed based on geometry elements
        GetGeometry(element, out List<DB.Mesh> meshes, out List<DB.Solid> solids);
        var filteredSolids = solids.Where(solid => solid.Volume > 0 && !solid.Faces.IsEmpty && solid.Faces.get_Item(0).MaterialElementId == material.Id);
        volume += filteredSolids.Sum(solid => solid.Volume);
        area  += filteredSolids
          .Select(solid => solid.Faces.Cast<Face>().Select(face => face.Area)
          .Max()).Sum();
      }
      // Convert revit interal units to speckle commit units
      double factor = ScaleToSpeckle(1);
      volume *= factor * factor * factor;
      area *= factor * factor;

      var speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
      var materialQuantity = new Objects.Other.MaterialQuantity(speckleMaterial, volume, area, units);

      if (LocationToSpeckle(element) is ICurve curve)
        materialQuantity["length"] = curve.length;
      else if (element is DB.Architecture.Railing)
        materialQuantity["length"] = (element as DB.Architecture.Railing).GetPath().Sum(e => e.Length)*factor;
      else if (element is DB.Architecture.ContinuousRail)
        materialQuantity["length"] = (element as DB.Architecture.ContinuousRail).GetPath().Sum(e => e.Length) * factor;
      return materialQuantity;
    }
    #endregion
    #region MaterialQuantities
    public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, string units)
    {
      ICollection<ElementId> matIDs = new List<ElementId>();
      if (element.Category.HasMaterialQuantities) matIDs = element?.GetMaterialIds(false);
      else if (element is MEPCurve)
      {
        DB.Material mepMaterial = ConverterRevit.GetMEPSystemRevitMaterial(element);
        if (mepMaterial != null) matIDs.Add(mepMaterial.Id);
      }
      else
      {
        matIDs = GetMaterialsFromGeometry(element)?.ToList();
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
    private IEnumerable<DB.ElementId> GetMaterialsFromGeometry(DB.Element element, bool isConverterAsInstance=false, DB.Options options=null)
    {
      GetGeometry(element, out List<DB.Mesh> meshes, out List<DB.Solid> solids);
      if (solids != null && solids.Any())
      {
        HashSet<ElementId> materialIds = new HashSet<ElementId>();
        return solids.Where(solid => solid.Volume > 0 && !solid.Faces.IsEmpty).Select(m => m.Faces.get_Item(0).MaterialElementId).Distinct();
      }
      else return null;
    }

    private void GetGeometry(DB.Element element, out List<DB.Mesh> meshes, out List<DB.Solid> solids, bool isConvertedAsInstance = false, DB.Options options = null)
    {
      options ??= new DB.Options();
      GeometryElement geom = null;
      solids = null;
      meshes = null;
      try
      {
        geom = element.get_Geometry(options);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException)
      {
        options.ComputeReferences = false;
        geom = element.get_Geometry(options);
      }
      if (geom != null)
      {
        List<DB.Solid> sol = new List<DB.Solid>();
        List<DB.Mesh> meshs = new List<DB.Mesh>();
        SortGeometry(element, sol, meshs, geom);
        solids = sol;
        meshes = meshs;
      }
    }
  }
}
