#nullable enable
using Autodesk.Revit.DB;
using ConverterRevitShared.Extensions;
using Objects.BuiltElements;
using Objects.Other;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    //#region MaterialQuantity
    ///// <summary>
    ///// Gets the quantitiy of a material in one element
    ///// </summary>
    ///// <param name="element"></param>
    ///// <param name="material"></param>
    ///// <returns></returns>
    //public Objects.Other.MaterialQuantity MaterialQuantityToSpeckle(DB.Element element, DB.Material material, string units)
    //{
    //  if (material == null || element == null)
    //    return null;
    //  double volume = 0;
    //  double area = 0;
    //  if (element is HostObject)
    //  {
    //    volume += element.GetMaterialVolume(material.Id);
    //    area += element.GetMaterialArea(material.Id, false);
    //  }
    //  else if (element is DB.Plumbing.Pipe || element is DB.Plumbing.FlexPipe)
    //  {
    //    // Area and Volume are computed based on geometry elements
    //    GetGeometry(element, out List<DB.Mesh> meshes, out List<DB.Solid> solids);
    //    volume += solids.Sum(s => s.Volume);
    //    area += solids.Select(s => s.Faces.Cast<DB.Face>().Select(face => face.Area).Max()).Sum();
    //  }
    //  else
    //  {
    //    // Area and Volume are computed based on geometry elements
    //    GetGeometry(element, out List<DB.Mesh> meshes, out List<DB.Solid> solids);
    //    var filteredSolids = solids.Where(solid => solid.Volume > 0 && !solid.Faces.IsEmpty && solid.Faces.get_Item(0).MaterialElementId == material.Id);
    //    volume += filteredSolids.Sum(solid => solid.Volume);
    //    area += filteredSolids
    //      .Select(solid => solid.Faces.Cast<Face>().Select(face => face.Area)
    //      .Max()).Sum();
    //  }
    //  // Convert revit interal units to speckle commit units
    //  double factor = ScaleToSpeckle(1);
    //  volume *= factor * factor * factor;
    //  area *= factor * factor;

    //  var speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
    //  var materialQuantity = new Objects.Other.MaterialQuantity(speckleMaterial, volume, area, units);

    //  if (LocationToSpeckle(element) is ICurve curve)
    //    materialQuantity["length"] = curve.length;
    //  else if (element is DB.Architecture.Railing)
    //    materialQuantity["length"] = (element as DB.Architecture.Railing).GetPath().Sum(e => e.Length) * factor;
    //  else if (element is DB.Architecture.ContinuousRail)
    //    materialQuantity["length"] = (element as DB.Architecture.ContinuousRail).GetPath().Sum(e => e.Length) * factor;
    //  return materialQuantity;
    //}
    //#endregion
    //#region MaterialQuantities
    //public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, string units)
    //{
    //  ICollection<ElementId> matIDs = new List<ElementId>();
    //  if (element.Category.HasMaterialQuantities) matIDs = element?.GetMaterialIds(false);
    //  else if (element is MEPCurve)
    //  {
    //    DB.Material mepMaterial = ConverterRevit.GetMEPSystemRevitMaterial(element);
    //    if (mepMaterial != null) matIDs.Add(mepMaterial.Id);
    //  }
    //  else
    //  {
    //    matIDs = GetMaterialsFromGeometry(element)?.ToList();
    //  }
    //  if (matIDs == null || !matIDs.Any())
    //    return null;
    //  var materials = matIDs.Select(material => element.Document.GetElement(material) as DB.Material);
    //  return MaterialQuantitiesToSpeckle(element, materials, units);
    //}

    public IEnumerable<Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, string units)
    {
      double factor = ScaleToSpeckle(1);

      IEnumerable<MaterialQuantity> quantitiesFromAPI = GetMaterialQuantitiesFromAPICall(element, units, factor); 
      if (quantitiesFromAPI.Any())
      {
        return quantitiesFromAPI;
      }
      else if (GetMaterialQuantityForMEPElement(element, units, factor) is MaterialQuantity quantity)
      {
        return new List<MaterialQuantity>() { quantity };
      }
      else
      {
        return GetMaterialQuantitiesFromSolids(element, units, factor);
      }
    }

    private IEnumerable<MaterialQuantity> GetMaterialQuantitiesFromAPICall(DB.Element element, string units, double factor)
    {
      foreach (ElementId matId in element.GetMaterialIds(false))
      {
        if (element.Document.GetElement(matId) is not DB.Material material)
        {
          continue;
        }

        Other.Material speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
        double volume = element.GetMaterialVolume(material.Id);
        double area = element.GetMaterialArea(material.Id, false);
        volume *= factor * factor * factor;
        area *= factor * factor;
        yield return new Objects.Other.MaterialQuantity(speckleMaterial, volume, area, units);
      }
    }

    private MaterialQuantity? GetMaterialQuantityForMEPElement(DB.Element element, string units, double factor)
    {
      DB.Material material = GetMEPSystemRevitMaterial(element);
      if (material == null)
      {
        return null;
      }

      GetGeometry(element, out _, out List<Solid> solids);
      Other.Material speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
      var (area, volume) = GetAreaAndVolume(solids, factor);
      return new MaterialQuantity(speckleMaterial, volume, area, units);
    }
    
    private IEnumerable<MaterialQuantity> GetMaterialQuantitiesFromSolids(DB.Element element, string units, double factor)
    {
      GetGeometry(element, out _, out List<Solid> solids);
      foreach (var matId in GetMaterialsFromSolids(element, solids))
      {
        if (element.Document.GetElement(matId) is not DB.Material material)
        {
          continue;
        }

        Other.Material speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
        var (area, volume) = GetAreaAndVolume(solids, factor, matId);
        yield return new MaterialQuantity(speckleMaterial, volume, area, units);
      }
    }

    private (double, double) GetAreaAndVolume(List<Solid> solids, double factor, ElementId? materialId = null)
    {
      if (materialId != null)
      {
        solids = solids
          .Where(
            solid => solid.Volume > 0 
            && !solid.Faces.IsEmpty 
            && solid.Faces.get_Item(0).MaterialElementId == materialId)
          .ToList();
      }

      double volume = solids.Sum(solid => solid.Volume);
      double area = solids
          .Select(solid => solid.Faces.Cast<Face>().Select(face => face.Area)
          .Max()).Sum();
      volume *= factor * factor * factor;
      area *= factor * factor;
      return (area, volume);
    }

    //public IEnumerable<Objects.Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, IEnumerable<DB.Material> materials, string units)
    //{
    //  if (materials == null || !materials.Any()) return null;
    //  List<Objects.Other.MaterialQuantity> quantities = new List<Objects.Other.MaterialQuantity>();
    //  foreach (var material in materials)
    //    quantities.Add(MaterialQuantityToSpeckle(element, material, units));
    //  return quantities;
    //}
    //#endregion
    private IEnumerable<DB.ElementId> GetMaterialsFromSolids(DB.Element element, List<Solid> solids)
    {
      return solids
        .Where(solid => solid.Volume > 0 && !solid.Faces.IsEmpty)
        .Select(m => m.Faces.get_Item(0).MaterialElementId)
        .Distinct();
    }

    private void GetGeometry(DB.Element element, out List<DB.Mesh> meshes, out List<DB.Solid> solids)
    {
      DB.Options options = new()
      {
        DetailLevel = ViewDetailLevel.Fine
      };
      GeometryElement geom;
      solids = new();
      meshes = new();
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
        SortGeometry(element, solids, meshes, geom);
      }
    }
  }
}
