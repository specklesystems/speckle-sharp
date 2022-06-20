using System;
using System.Collections.Generic;
using Objects.Structural.Materials;
using CSiAPIv1;

namespace Objects.Converter.CSI

{
  public partial class ConverterCSI
  {
    public object MaterialToNative(Objects.Structural.Materials.Material material)
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

      //Material Problem 
      if (material.designCode != null)
      {
        Model.PropMaterial.AddMaterial(ref materialName, eMaterialType, material.designCode, material.codeYear, material.grade);
        Model.PropMaterial.ChangeName(materialName, material.name);
      }
      else
      {
        Model.PropMaterial.SetMaterial(material.name, eMaterialType);
      }
      return material.name;
    }

    public Structural.Materials.Material MaterialToSpeckle(string name)
    {
      var speckleStructMaterial = new Structural.Materials.Material();
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
          speckleStructMaterial.materialType = Structural.MaterialType.Steel;
          GetSteelMaterial(name, ref speckleStructMaterial);
          break;
        case eMatType.Concrete:
          speckleStructMaterial.materialType = Structural.MaterialType.Concrete;
          GetConcreteMaterial(name, ref speckleStructMaterial);
          break;
        case eMatType.NoDesign:
          speckleStructMaterial.materialType = Structural.MaterialType.Other;
          break;
        case eMatType.Aluminum:
          speckleStructMaterial.materialType = Structural.MaterialType.Aluminium;
          break;
        case eMatType.Rebar:
          speckleStructMaterial.materialType = Structural.MaterialType.Rebar;
          GetRebarMaterial(name, ref speckleStructMaterial);
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
    public void GetSteelMaterial(string materialName, ref Structural.Materials.Material speckleMaterial)
    {
      double fy, fu, eFy, eFu, strainAtHardening, strainAtMaxStress, strainAtRupture, finalSlope;
      fy = fu = eFy = eFu = strainAtHardening = strainAtMaxStress = strainAtRupture = finalSlope = 0;
      int sStype, sSHysType;
      sStype = sSHysType = 0;


      Model.PropMaterial.GetOSteel_1(materialName, ref fy, ref fu, ref eFy, ref eFu, ref sStype, ref sSHysType, ref strainAtHardening, ref strainAtMaxStress, ref strainAtRupture, ref finalSlope);

      speckleMaterial.strength = fy;

      // Material is isotropic or elastic - No support for other types currently
      if (sSHysType == 7 || sSHysType == 1)
      {
        GetIsotropicMaterial(materialName, ref speckleMaterial);
      }
    }

    public void GetConcreteMaterial(string materialName, ref Structural.Materials.Material speckleMaterial)
    {
      double fc, fcsFactor, strainAtFc, strainUltimate, finalSlope, frictionAngle, dilatationalAngle;
      fc = fcsFactor = strainAtFc = strainUltimate = finalSlope = frictionAngle = dilatationalAngle = 0;
      int sStype, sSHysType;
      sStype = sSHysType = 0;
      bool isLightweight = false;

      Model.PropMaterial.GetOConcrete_1(materialName, ref fc, ref isLightweight, ref fcsFactor, ref sStype, ref sSHysType, ref strainAtFc, ref strainUltimate, ref finalSlope, ref frictionAngle, ref dilatationalAngle);

      speckleMaterial.strength = fc;

      // Material is isotropic - No support for other types currently
      if (sSHysType == 7 || sSHysType == 1 || sSHysType == 4)
      {
        GetIsotropicMaterial(materialName, ref speckleMaterial);
      }
    }

    public void GetRebarMaterial(string materialName, ref Structural.Materials.Material speckleMaterial)
    {
      double fy, fu, eFy, eFu, strainAtHardening, strainUltimate, finalSlope;
      fy = fu = eFy = eFu = strainAtHardening = strainUltimate = finalSlope = 0;
      int sStype, sSHysType;
      sStype = sSHysType = 0;
      bool useCaltransSSDefaults = false;

      Model.PropMaterial.GetORebar_1(materialName, ref fy, ref fu, ref eFy, ref eFu, ref sStype, ref sSHysType, ref strainAtHardening, ref strainUltimate, ref finalSlope, ref useCaltransSSDefaults);

      speckleMaterial.strength = fy;

      // Rebar can only be set to uniaxial
      GetUniaxialMaterial(materialName, ref speckleMaterial);
    }

    public void GetIsotropicMaterial(string materialName, ref Structural.Materials.Material speckleMaterial)
    {
      double e, u, a, g;
      e = u = a = g = 0;

      Model.PropMaterial.GetMPIsotropic(materialName, ref e, ref u, ref a, ref g);

      speckleMaterial.elasticModulus = e;
      speckleMaterial.poissonsRatio = u;
      speckleMaterial.thermalExpansivity = a;
      speckleMaterial.shearModulus = g;
    }

    public void GetUniaxialMaterial(string materialName, ref Structural.Materials.Material speckleMaterial)
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