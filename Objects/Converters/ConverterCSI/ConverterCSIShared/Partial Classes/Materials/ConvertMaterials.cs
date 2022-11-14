using System;
using System.Collections.Generic;
using Objects.Structural.Materials;
using CSiAPIv1;

namespace Objects.Converter.CSI

{
  public partial class ConverterCSI
  {
    public object MaterialToNative(Objects.Structural.Materials.StructuralMaterial material)
    {
      var matType = material.materialType;
      var eMaterialType = eMatType.Steel;
      switch (matType)
      {
        case Structural.MaterialType.Steel:
          eMaterialType = eMatType.Steel;
          break;
        case Structural.MaterialType.Concrete:
          eMaterialType = eMatType.Concrete;
          break;
        case Structural.MaterialType.Other:
          eMaterialType = eMatType.NoDesign;
          break;
        case Structural.MaterialType.Aluminium:
          eMaterialType = eMatType.Aluminum;
          break;
        case Structural.MaterialType.Rebar:
          eMaterialType = eMatType.Rebar;
          break;
        case Structural.MaterialType.ColdFormed:
          eMaterialType = eMatType.ColdFormed;
          break;
        case Structural.MaterialType.Tendon:
          eMaterialType = eMatType.Tendon;
          break;
        case Structural.MaterialType.Masonry:
          eMaterialType = eMatType.Masonry;
          break;
      }
      string materialName = material.name;

      if (material.designCode != null)
      {
        Model.PropMaterial.AddMaterial(ref materialName, eMaterialType, material.designCode, material.codeYear, material.grade);
        Model.PropMaterial.ChangeName(materialName, material.name);
      }
      else
      {
        Model.PropMaterial.SetMaterial(material.name, eMaterialType);
        if(material is Structural.CSI.Materials.CSIConcrete){
          SetConcreteMaterial((Structural.CSI.Materials.CSIConcrete)material, material.name);
        }
        else if (material is Structural.CSI.Materials.CSISteel){
          SetSteelMaterial((Structural.CSI.Materials.CSISteel)material, material.name);
        }
      }
      return material.name;
    }

    public Structural.Materials.StructuralMaterial MaterialToSpeckle(string name)
    {
      var speckleStructMaterial = new Structural.Materials.StructuralMaterial();
      speckleStructMaterial.name = name;
      eMatType matType = new eMatType();
      int color = 0;
      string notes, GUID;
      notes = GUID = null;
      Model.PropMaterial.GetMaterial(name, ref matType, ref color, ref notes, ref GUID);

      speckleStructMaterial.applicationId = GUID;

      switch (matType)
      {
        case eMatType.Steel:
          return GetSteelMaterial(name);
          break;
        case eMatType.Concrete:
          speckleStructMaterial.materialType = Structural.MaterialType.Concrete;
          return GetConcreteMaterial(name);
          break;
        case eMatType.NoDesign:
          speckleStructMaterial.materialType = Structural.MaterialType.Other;
          break;
        case eMatType.Aluminum:
          speckleStructMaterial.materialType = Structural.MaterialType.Aluminium;
          break;
        case eMatType.Rebar:
    
          return GetRebarMaterial(name);
          break;
        case eMatType.ColdFormed:
          speckleStructMaterial.materialType = Structural.MaterialType.ColdFormed;
          break;
        case eMatType.Tendon:
          speckleStructMaterial.materialType = Structural.MaterialType.Tendon;
          break;
        case eMatType.Masonry:
          speckleStructMaterial.materialType = Structural.MaterialType.Masonry;
          break;
      }
      
      return speckleStructMaterial;
    }

    #region Helper functions
    public Structural.CSI.Materials.CSISteel GetSteelMaterial(string materialName)
    {
      double fy, fu, eFy, eFu, strainAtHardening, strainAtMaxStress, strainAtRupture, finalSlope;
      fy = fu = eFy = eFu = strainAtHardening = strainAtMaxStress = strainAtRupture = finalSlope = 0;
      int sStype, sSHysType;
      sStype = sSHysType = 0;

      var speckleMaterial = new Structural.CSI.Materials.CSISteel();
    

      // Material is isotropic or elastic - No support for other types currently
      if (sSHysType == 7 || sSHysType == 1)
      {
        speckleMaterial = (Structural.CSI.Materials.CSISteel)GetIsotropicMaterial(materialName);
      }

      Model.PropMaterial.GetOSteel_1(materialName, ref fy, ref fu, ref eFy, ref eFu, ref sStype, ref sSHysType, ref strainAtHardening, ref strainAtMaxStress, ref strainAtRupture, ref finalSlope);
  
      speckleMaterial.strength = fy;
      speckleMaterial.ultimateStrength = fu;
      speckleMaterial.maxStrain = strainAtRupture;
      speckleMaterial.strainHardeningModulus = finalSlope ;
      speckleMaterial.strainAtHardening = strainAtHardening;
      speckleMaterial.strainAtMaxStress = strainAtMaxStress;
      speckleMaterial.SSHysType = sSHysType;
      speckleMaterial.SSType = sStype;
      speckleMaterial.EFu = eFu;
      speckleMaterial.EFy = eFy;
      speckleMaterial.materialType = Structural.MaterialType.Steel;

      return speckleMaterial;
    }
    public void SetSteelMaterial(Structural.CSI.Materials.CSISteel structuralMaterial, string etabsMaterialName){
      //Support only isotropic Steel Material setting
      //Lossy transformation since Speckle classes are missing material properties
      Model.PropMaterial.SetOSteel_1(etabsMaterialName, 
      structuralMaterial.strength, 
      structuralMaterial.yieldStrength,
      structuralMaterial.EFy,
      structuralMaterial.EFu,
      structuralMaterial.SSType,
      structuralMaterial.SSHysType,
      structuralMaterial.strainAtHardening,
      structuralMaterial.strainAtMaxStress,
      structuralMaterial.maxStrain,
      structuralMaterial.strainHardeningModulus);

      Model.PropMaterial.SetMPIsotropic(etabsMaterialName,
      structuralMaterial.elasticModulus,
      structuralMaterial.poissonsRatio, 
      structuralMaterial.thermalExpansivity, 
      structuralMaterial.shearModulus);
    
     }

    public Structural.CSI.Materials.CSIConcrete GetConcreteMaterial(string materialName)
    {
      double fc, fcsFactor, strainAtFc, strainUltimate, finalSlope, frictionAngle, dilatationalAngle;
      fc = fcsFactor = strainAtFc = strainUltimate = finalSlope = frictionAngle = dilatationalAngle = 0;
      int sStype, sSHysType;
      sStype = sSHysType = 0;
      bool isLightweight = false;

      var speckleMaterial = new Structural.CSI.Materials.CSIConcrete();
      // Material is isotropic - No support for other types currently
      if (sSHysType == 7 || sSHysType == 1 || sSHysType == 4)
      {
        speckleMaterial = (Structural.CSI.Materials.CSIConcrete)GetIsotropicMaterial(materialName);
      }

      Model.PropMaterial.GetOConcrete_1(materialName, ref fc, ref isLightweight, ref fcsFactor, ref sStype, ref sSHysType, ref strainAtFc, ref strainUltimate, ref finalSlope, ref frictionAngle, ref dilatationalAngle);


      
      speckleMaterial.strength = fc;
      speckleMaterial.materialSafetyFactor = fcsFactor;
      speckleMaterial.maxCompressiveStrain = strainUltimate;
      speckleMaterial.maxTensileStrain = strainAtFc;
      speckleMaterial.finalSlope = finalSlope;
      speckleMaterial.lightweight = isLightweight;
      speckleMaterial.SSHysType = sSHysType;
      speckleMaterial.SSType = sStype;
      speckleMaterial.frictionAngle = frictionAngle;
      speckleMaterial.dialationalAngle = dilatationalAngle;
      speckleMaterial.materialType = Structural.MaterialType.Concrete;

      return speckleMaterial;
    }

    public void SetConcreteMaterial(Structural.CSI.Materials.CSIConcrete structuralMaterial, string etabsMaterialName){
      Model.PropMaterial.SetOConcrete_1(etabsMaterialName,
      structuralMaterial.strength,
      structuralMaterial.lightweight, 
      structuralMaterial.materialSafetyFactor,
      structuralMaterial.SSType,
      structuralMaterial.SSHysType,
      structuralMaterial.maxTensileStrain,
      structuralMaterial.maxCompressiveStrain,
      structuralMaterial.finalSlope,
      structuralMaterial.frictionAngle,
      structuralMaterial.dialationalAngle);


      Model.PropMaterial.SetMPIsotropic(etabsMaterialName,
      structuralMaterial.elasticModulus,
      structuralMaterial.poissonsRatio,
      structuralMaterial.thermalExpansivity,
      structuralMaterial.shearModulus);
    }

    public Structural.CSI.Materials.CSIRebar GetRebarMaterial(string materialName ){ 
      double fy, fu, eFy, eFu, strainAtHardening, strainUltimate, finalSlope;
      fy = fu = eFy = eFu = strainAtHardening = strainUltimate = finalSlope = 0;
      int sStype, sSHysType;
      sStype = sSHysType = 0;
      bool useCaltransSSDefaults = false;


      Structural.CSI.Materials.CSIRebar rebarMaterial = new Structural.CSI.Materials.CSIRebar();
      // Rebar can only be set to uniaxial
      //GetUniaxialMaterial(materialName);
      //speckleMaterial = (Structural.CSI.Materials.CSIRebar)speckleMaterial;
      //Model.PropMaterial.GetORebar_1(materialName, ref fy, ref fu, ref eFy, ref eFu, ref sStype, ref sSHysType, ref strainAtHardening, ref strainUltimate, ref finalSlope, ref useCaltransSSDefaults);

      //speckleMaterial.strength = fy;
      return rebarMaterial;

    }

    public void SetRebarMaterial(StructuralMaterial structuralMaterial, string etabsMaterialName){
      Model.PropMaterial.SetORebar_1(etabsMaterialName, structuralMaterial.strength, 0, 0, 0, 0, 0, 0, 0, 0, false);
      Model.PropMaterial.SetMPUniaxial(etabsMaterialName, structuralMaterial.elasticModulus, structuralMaterial.thermalExpansivity);
    }

    public object GetIsotropicMaterial(string materialName)
    {
      double e, u, a, g;
      e = u = a = g = 0;

      Model.PropMaterial.GetMPIsotropic(materialName, ref e, ref u, ref a, ref g);
      var speckleMaterial = new Structural.Materials.StructuralMaterial();
      speckleMaterial.elasticModulus = e;
      speckleMaterial.poissonsRatio = u;
      speckleMaterial.thermalExpansivity = a;
      speckleMaterial.shearModulus = g;
      return speckleMaterial;
    }
  
    public void GetUniaxialMaterial(string materialName,ref Structural.Materials.StructuralMaterial speckleMaterial)
    {
      double e = 0;
      double a = 0;

      Model.PropMaterial.GetMPUniaxial(materialName, ref e, ref a);

      speckleMaterial.elasticModulus = e;
      speckleMaterial.thermalExpansivity = a;
    }
    #endregion
  }
}