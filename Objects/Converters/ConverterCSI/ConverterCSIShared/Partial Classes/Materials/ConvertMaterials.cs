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
      switch (matType)
      {
        case eMatType.Steel:
          speckleStructMaterial.materialType = Structural.MaterialType.Steel;
          break;
        case eMatType.Concrete:
          speckleStructMaterial.materialType = Structural.MaterialType.Concrete;
          break;
        case eMatType.NoDesign:
          speckleStructMaterial.materialType = Structural.MaterialType.Other;
          break;
        case eMatType.Aluminum:
          speckleStructMaterial.materialType = Structural.MaterialType.Aluminium;
          break;
        case eMatType.Rebar:
          speckleStructMaterial.materialType = Structural.MaterialType.Rebar;
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
  }
}