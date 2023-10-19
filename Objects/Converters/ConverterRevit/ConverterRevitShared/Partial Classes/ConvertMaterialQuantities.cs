#nullable enable
using Autodesk.Revit.DB;
using ConverterRevitShared.Extensions;
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
 
      if (MaterialAreaAPICallWillReportSingleFace(element))
      {
        return GetMaterialQuantitiesFromAPICall(element, units, factor);
      }
      else if (element.IsMEPElement())
      {
        List<MaterialQuantity> quantities = new();
        MaterialQuantity quantity = GetMaterialQuantityForMEPElement(element, units, factor);
        if (quantity != null)
        {
          quantities.Add(quantity);
        }
        return quantities;
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

      DB.Options options = new() { DetailLevel = ViewDetailLevel.Fine };
      var (solids, _) = GetSolidsAndMeshesFromElement(element, options);

      Other.Material speckleMaterial = ConvertAndCacheMaterial(material.Id, material.Document);
      var (area, volume) = GetAreaAndVolumeFromSolids(solids, factor);
      return new MaterialQuantity(speckleMaterial, volume, area, units);
    }
    
    private IEnumerable<MaterialQuantity> GetMaterialQuantitiesFromSolids(DB.Element element, string units, double factor)
    {
      DB.Options options = new() { DetailLevel = ViewDetailLevel.Fine };
      var (solids, _) = GetSolidsAndMeshesFromElement(element, options);

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
      IEnumerable<double> areaOfLargestFaceInEachSolid = solids
          .Select(solid => solid.Faces.Cast<Face>().Select(face => face.Area)
          .Max());
      double area = areaOfLargestFaceInEachSolid.Sum();
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
    
    private bool MaterialAreaAPICallWillReportSingleFace(Element element)
    {
      return element switch
      {
        DB.CeilingAndFloor => true,
        DB.Wall => true,
        DB.RoofBase => true,
        _ => false
      };
    }
  }
}
