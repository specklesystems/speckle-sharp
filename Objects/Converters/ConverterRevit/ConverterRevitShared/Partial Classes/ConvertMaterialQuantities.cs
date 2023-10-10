#nullable enable
using Autodesk.Revit.DB;
using Objects.Other;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
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
        Other.Material speckleMaterial = ConvertAndCacheMaterial(matId, element.Document);
        double volume = element.GetMaterialVolume(matId);
        double area = element.GetMaterialArea(matId, false);
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
      var (area, volume) = GetAreaAndVolumeFromSolids(solids, factor);
      return new MaterialQuantity(speckleMaterial, volume, area, units);
    }
    
    private IEnumerable<MaterialQuantity> GetMaterialQuantitiesFromSolids(DB.Element element, string units, double factor)
    {
      GetGeometry(element, out _, out List<Solid> solids);
      foreach (ElementId matId in GetMaterialsFromSolids(solids))
      {
        Other.Material speckleMaterial = ConvertAndCacheMaterial(matId, element.Document);
        var (area, volume) = GetAreaAndVolumeFromSolids(solids, factor, matId);
        yield return new MaterialQuantity(speckleMaterial, volume, area, units);
      }
    }

    private (double, double) GetAreaAndVolumeFromSolids(List<Solid> solids, double factor, ElementId? materialId = null)
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

    private IEnumerable<DB.ElementId> GetMaterialsFromSolids(List<Solid> solids)
    {
      return solids
        .Where(solid => solid.Volume > 0 && !solid.Faces.IsEmpty)
        .Select(m => m.Faces.get_Item(0).MaterialElementId)
        .Distinct();
    }

    private void GetGeometry(DB.Element element, out List<DB.Mesh> meshes, out List<DB.Solid> solids)
    {
      DB.Options options = new() { DetailLevel = ViewDetailLevel.Fine };
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
