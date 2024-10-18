using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.Structural.Materials;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  private StructuralMaterial GetStructuralMaterial(Material material)
  {
    if (material == null)
    {
      return null;
    }

    StructuralAsset materialAsset = null;
    string name = null;
    if (material.StructuralAssetId != ElementId.InvalidElementId)
    {
      materialAsset = (
        (PropertySetElement)material.Document.GetElement(material.StructuralAssetId)
      ).GetStructuralAsset();

      name = material.Document.GetElement(material.StructuralAssetId)?.Name;
    }
    var materialName = material.MaterialClass;
    var materialType = GetMaterialType(materialName);

    var speckleMaterial = GetStructuralMaterial(materialType, materialAsset, name);
    speckleMaterial.applicationId = material.UniqueId;

    return speckleMaterial;
  }

  private StructuralMaterial GetStructuralMaterial(
    StructuralMaterialType materialType,
    StructuralAsset materialAsset,
    string name
  )
  {
    Structural.Materials.StructuralMaterial speckleMaterial = null;

    if (materialType == StructuralMaterialType.Undefined && materialAsset != null)
    {
      materialType = GetMaterialType(materialAsset);
    }

    name ??= materialType.ToString();
    switch (materialType)
    {
      case StructuralMaterialType.Concrete:
        var concreteMaterial = new Concrete { name = name, materialType = Structural.MaterialType.Concrete, };

        if (materialAsset != null)
        {
          concreteMaterial.compressiveStrength = materialAsset.ConcreteCompression; // Newtons per foot meter
          concreteMaterial.lightweight = materialAsset.Lightweight;
        }

        speckleMaterial = concreteMaterial;
        break;
      case StructuralMaterialType.Steel:
        var steelMaterial = new Steel
        {
          name = name,
          materialType = Structural.MaterialType.Steel,
          designCode = null,
          codeYear = null,
          maxStrain = 0,
          dampingRatio = 0,
        };

        if (materialAsset != null)
        {
          steelMaterial.grade = materialAsset.Name;
          steelMaterial.yieldStrength = materialAsset.MinimumYieldStress; // Newtons per foot meter
          steelMaterial.ultimateStrength = materialAsset.MinimumTensileStrength; // Newtons per foot meter
        }

        speckleMaterial = steelMaterial;
        break;
      case StructuralMaterialType.Wood:
        var timberMaterial = new Timber
        {
          name = name,
          materialType = Structural.MaterialType.Timber,
          designCode = null,
          codeYear = null,
          dampingRatio = 0
        };

        if (materialAsset != null)
        {
          timberMaterial.grade = materialAsset.WoodGrade;
          timberMaterial.species = materialAsset.WoodSpecies;
          timberMaterial["bendingStrength"] = materialAsset.WoodBendingStrength;
          timberMaterial["parallelCompressionStrength"] = materialAsset.WoodParallelCompressionStrength;
          timberMaterial["parallelShearStrength"] = materialAsset.WoodParallelShearStrength;
          timberMaterial["perpendicularCompressionStrength"] = materialAsset.WoodPerpendicularCompressionStrength;
          timberMaterial["perpendicularShearStrength"] = materialAsset.WoodPerpendicularShearStrength;
        }

        speckleMaterial = timberMaterial;
        break;
      default:
        var defaultMaterial = new Objects.Structural.Materials.StructuralMaterial { name = name, };
        speckleMaterial = defaultMaterial;
        break;
    }

    // TODO: support non-isotropic materials
    if (materialAsset != null)
    {
      // some of these are actually the dumbest units I've ever heard of
      speckleMaterial.elasticModulus = materialAsset.YoungModulus.X; // Newtons per foot meter
      speckleMaterial.poissonsRatio = materialAsset.PoissonRatio.X; // Unitless
      speckleMaterial.shearModulus = materialAsset.ShearModulus.X; // Newtons per foot meter
      speckleMaterial.density = materialAsset.Density; // kilograms per cubed feet
      speckleMaterial.thermalExpansivity = materialAsset.ThermalExpansionCoefficient.X; // inverse Kelvin
    }

    return speckleMaterial;
  }

  private StructuralMaterialType GetMaterialType(string materialName)
  {
    StructuralMaterialType materialType = StructuralMaterialType.Undefined;
    switch (materialName.ToLower())
    {
      case "concrete":
        materialType = StructuralMaterialType.Concrete;
        break;
      case "steel":
        materialType = StructuralMaterialType.Steel;
        break;
      case "wood":
        materialType = StructuralMaterialType.Wood;
        break;
    }

    return materialType;
  }

  private StructuralMaterialType GetMaterialType(StructuralAsset materialAsset)
  {
    StructuralMaterialType materialType = StructuralMaterialType.Undefined;
    switch (materialAsset?.StructuralAssetClass)
    {
      case StructuralAssetClass.Metal:
        materialType = StructuralMaterialType.Steel;
        break;
      case StructuralAssetClass.Concrete:
        materialType = StructuralMaterialType.Concrete;
        break;
      case StructuralAssetClass.Wood:
        materialType = StructuralMaterialType.Wood;
        break;
    }

    return materialType;
  }
}
