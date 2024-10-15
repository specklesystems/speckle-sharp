using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Objects.Structural.Materials;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public StructuralMaterial GetStructuralMaterial(Material material)
  {
    if (material == null)
    {
      return null;
    }

    StructuralAsset materialAsset = null;
    string name = null;

    if (material.StructuralAssetId != ElementId.InvalidElementId)
    {
      var propertySetElement = material.Document.GetElement(material.StructuralAssetId) as PropertySetElement;
      materialAsset = propertySetElement?.GetStructuralAsset();
      name = propertySetElement?.Name;
    }

    var materialType = GetMaterialType(material.MaterialClass);
    var speckleMaterial = CreateSpeckleMaterial(materialType, materialAsset, name);
    speckleMaterial.applicationId = material.UniqueId;

    return speckleMaterial;
  }

  public StructuralMaterial CreateSpeckleMaterial(
    StructuralMaterialType materialType,
    StructuralAsset materialAsset,
    string name
  )
  {
    Structural.Materials.StructuralMaterial speckleMaterial = materialType switch
    {
      StructuralMaterialType.Concrete => CreateConcreteMaterial(materialAsset, name ?? materialType.ToString()),
      StructuralMaterialType.Steel => CreateSteelMaterial(materialAsset, name ?? materialType.ToString()),
      StructuralMaterialType.Wood => CreateTimberMaterial(materialAsset, name ?? materialType.ToString()),
      StructuralMaterialType.Undefined => throw new System.NotImplementedException(),
      StructuralMaterialType.Other => throw new System.NotImplementedException(),
      StructuralMaterialType.PrecastConcrete => throw new System.NotImplementedException(),
      StructuralMaterialType.Generic => throw new System.NotImplementedException(),
      StructuralMaterialType.Aluminum => throw new System.NotImplementedException(),
      _ => new Objects.Structural.Materials.StructuralMaterial { name = name ?? materialType.ToString() }
    };

    if (materialAsset != null)
    {
      SetCommonMaterialProperties(speckleMaterial, materialAsset);
    }

    return speckleMaterial;
  }

  public Concrete CreateConcreteMaterial(StructuralAsset materialAsset, string name)
  {
    var concrete = new Concrete { name = name, materialType = Structural.MaterialType.Concrete, };

    if (materialAsset != null)
    {
#if REVIT2020 || REVIT2021 || REVIT2022
      concrete.compressiveStrength = materialAsset.ConcreteCompression;
#else
      concrete.compressiveStrength = ScaleToSpeckle(materialAsset.ConcreteCompression, RevitStressTypeId);
#endif
      concrete.lightweight = materialAsset.Lightweight;
    }

    return concrete;
  }

  public Steel CreateSteelMaterial(StructuralAsset materialAsset, string name)
  {
    var steel = new Steel
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
      steel.grade = materialAsset.Name;
#if REVIT2020 || REVIT2021 || REVIT2022
        steel.yieldStrength = materialAsset.MinimumYieldStress;
        steel.ultimateStrength = materialAsset.MinimumTensileStrength;
#else
      steel.yieldStrength = ScaleToSpeckle(materialAsset.MinimumYieldStress, RevitStressTypeId);
      steel.ultimateStrength = ScaleToSpeckle(materialAsset.MinimumTensileStrength, RevitStressTypeId);
#endif
    }

    return steel;
  }

  public Timber CreateTimberMaterial(StructuralAsset materialAsset, string name)
  {
    var timber = new Timber
    {
      name = name,
      materialType = Structural.MaterialType.Timber,
      designCode = null,
      codeYear = null,
      dampingRatio = 0,
    };

    if (materialAsset != null)
    {
      timber.grade = materialAsset.WoodGrade;
      timber.species = materialAsset.WoodSpecies;
#if REVIT2020 || REVIT2021 || REVIT2022
        timber["bendingStrength"] = materialAsset.WoodBendingStrength;
        timber["parallelCompressionStrength"] = materialAsset.WoodParallelCompressionStrength;
        timber["parallelShearStrength"] = materialAsset.WoodParallelShearStrength;
        timber["perpendicularCompressionStrength"] = materialAsset.WoodPerpendicularCompressionStrength;
        timber["perpendicularShearStrength"] = materialAsset.WoodPerpendicularShearStrength;
#else
      timber["bendingStrength"] = ScaleToSpeckle(materialAsset.WoodBendingStrength, RevitStressTypeId);
      timber["parallelCompressionStrength"] = ScaleToSpeckle(
        materialAsset.WoodParallelCompressionStrength,
        RevitStressTypeId
      );
      timber["parallelShearStrength"] = ScaleToSpeckle(materialAsset.WoodParallelShearStrength, RevitStressTypeId);
      timber["perpendicularCompressionStrength"] = ScaleToSpeckle(
        materialAsset.WoodPerpendicularCompressionStrength,
        RevitStressTypeId
      );
      timber["perpendicularShearStrength"] = ScaleToSpeckle(
        materialAsset.WoodPerpendicularShearStrength,
        RevitStressTypeId
      );
#endif
    }

    return timber;
  }

  public void SetCommonMaterialProperties(
    Structural.Materials.StructuralMaterial speckleMaterial,
    StructuralAsset materialAsset
  )
  {
#if REVIT2020 || REVIT2021 || REVIT2022
    speckleMaterial.elasticModulus = materialAsset.YoungModulus.X;
    speckleMaterial.shearModulus = materialAsset.ShearModulus.X;
    speckleMaterial.density = materialAsset.Density;
#else
    speckleMaterial.elasticModulus = ScaleToSpeckle(materialAsset.YoungModulus.X, RevitStressTypeId);
    speckleMaterial.shearModulus = ScaleToSpeckle(materialAsset.ShearModulus.X, RevitStressTypeId);
    speckleMaterial.density = ScaleToSpeckle(materialAsset.Density, RevitDensityTypeId);
#endif
    speckleMaterial.poissonsRatio = materialAsset.PoissonRatio.X; // Unitless
    speckleMaterial.thermalExpansivity = materialAsset.ThermalExpansionCoefficient.X;
  }

  public StructuralMaterialType GetMaterialType(string materialName) =>
    materialName.ToLower() switch
    {
      "concrete" => StructuralMaterialType.Concrete,
      "steel" => StructuralMaterialType.Steel,
      "wood" => StructuralMaterialType.Wood,
      _ => StructuralMaterialType.Undefined
    };

  public StructuralMaterialType GetMaterialType(StructuralAsset materialAsset) =>
    materialAsset?.StructuralAssetClass switch
    {
      StructuralAssetClass.Metal => StructuralMaterialType.Steel,
      StructuralAssetClass.Concrete => StructuralMaterialType.Concrete,
      StructuralAssetClass.Wood => StructuralMaterialType.Wood,
      StructuralAssetClass.Undefined => throw new System.NotImplementedException(),
      StructuralAssetClass.Basic => throw new System.NotImplementedException(),
      StructuralAssetClass.Generic => throw new System.NotImplementedException(),
      StructuralAssetClass.Liquid => throw new System.NotImplementedException(),
      StructuralAssetClass.Gas => throw new System.NotImplementedException(),
      StructuralAssetClass.Plastic => throw new System.NotImplementedException(),
      null => throw new System.NotImplementedException(),
      _ => StructuralMaterialType.Undefined
    };
}
