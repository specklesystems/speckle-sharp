#nullable enable
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.Other;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  /// <summary>
  /// Material Quantities in Revit are stored in different ways and therefore need to be retrieved
  /// using different methods. According to this forum post https://forums.autodesk.com/t5/revit-api-forum/method-getmaterialarea-appears-to-use-different-formulas-for/td-p/11988215
  /// "Hosts" (whatever that means) will return the area of a single side of the object while other
  /// objects will return the combined area of every side of the element. Certain MEP element materials
  /// are attached to the MEP system that the element belongs to.
  /// </summary>
  /// <param name="element"></param>
  /// <param name="units"></param>
  /// <returns></returns>
  public IEnumerable<Other.MaterialQuantity> MaterialQuantitiesToSpeckle(DB.Element element, string units)
  {
    if (MaterialAreaAPICallWillReportSingleFace(element))
    {
      return GetMaterialQuantitiesFromAPICall(element, units);
    }
    else if (MaterialIsAttachedToMEPSystem(element))
    {
      MaterialQuantity quantity = GetMaterialQuantityForMEPElement(element, units);
      return quantity == null ? Enumerable.Empty<MaterialQuantity>() : new List<MaterialQuantity>() { quantity };
    }
    else
    {
      return GetMaterialQuantitiesFromSolids(element, units);
    }
  }

  private IEnumerable<MaterialQuantity> GetMaterialQuantitiesFromAPICall(DB.Element element, string units)
  {
    foreach (ElementId matId in element.GetMaterialIds(false))
    {
      double volume = element.GetMaterialVolume(matId);
      double area = element.GetMaterialArea(matId, false);
      yield return CreateMaterialQuantity(element, matId, area, volume, units);
    }
  }

  private MaterialQuantity? GetMaterialQuantityForMEPElement(DB.Element element, string units)
  {
    DB.Material material = GetMEPSystemRevitMaterial(element);
    if (material == null)
    {
      return null;
    }

    DB.Options options = new() { DetailLevel = ViewDetailLevel.Fine };
    var (solids, _) = GetSolidsAndMeshesFromElement(element, options);

    (double area, double volume) = GetAreaAndVolumeFromSolids(solids);
    return CreateMaterialQuantity(element, material.Id, area, volume, units);
  }

  private IEnumerable<MaterialQuantity> GetMaterialQuantitiesFromSolids(DB.Element element, string units)
  {
    DB.Options options = new() { DetailLevel = ViewDetailLevel.Fine };
    var (solids, _) = GetSolidsAndMeshesFromElement(element, options);

    foreach (ElementId matId in GetMaterialsFromSolids(solids))
    {
      (double area, double volume) = GetAreaAndVolumeFromSolids(solids, matId);
      yield return CreateMaterialQuantity(element, matId, area, volume, units);
    }
  }

  private MaterialQuantity CreateMaterialQuantity(
    Element element,
    ElementId materialId,
    double areaRevitInternalUnits,
    double volumeRevitInternalUnits,
    string units
  )
  {
    Other.Material speckleMaterial = ConvertAndCacheMaterial(materialId, element.Document);
    double factor = ScaleToSpeckle(1);
    double area = factor * factor * areaRevitInternalUnits;
    double volume = factor * factor * factor * volumeRevitInternalUnits;
    MaterialQuantity materialQuantity = new(speckleMaterial, volume, area, units);

    switch (element)
    {
      case DB.Architecture.Railing railing:
        materialQuantity["length"] = railing.GetPath().Sum(e => e.Length) * factor;
        break;

      case DB.Architecture.ContinuousRail continuousRail:
        materialQuantity["length"] = continuousRail.GetPath().Sum(e => e.Length) * factor;
        break;

      default:
        if (LocationToSpeckle(element) is ICurve curve)
        {
          materialQuantity["length"] = curve.length;
        }
        break;
    }
    ;

    return materialQuantity;
  }

  private (double, double) GetAreaAndVolumeFromSolids(List<Solid> solids, ElementId? materialId = null)
  {
    if (materialId != null)
    {
      solids = solids
        .Where(solid =>
          solid.Volume > 0 && !solid.Faces.IsEmpty && solid.Faces.get_Item(0).MaterialElementId == materialId
        )
        .ToList();
    }

    double volume = solids.Sum(solid => solid.Volume);
    IEnumerable<double> areaOfLargestFaceInEachSolid = solids.Select(solid =>
      solid.Faces.Cast<Face>().Select(face => face.Area).Max()
    );
    double area = areaOfLargestFaceInEachSolid.Sum();
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
      DB.CeilingAndFloor or DB.Wall or DB.RoofBase => true,
      _ => false
    };
  }

  private bool MaterialIsAttachedToMEPSystem(Element element)
  {
    return element switch
    {
      DB.Mechanical.Duct or DB.Mechanical.FlexDuct or DB.Plumbing.Pipe or DB.Plumbing.FlexPipe => true,
      _ => false
    };
  }
}
