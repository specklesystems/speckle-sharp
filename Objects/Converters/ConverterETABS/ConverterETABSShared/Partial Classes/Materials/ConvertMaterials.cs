using System;
using System.Collections.Generic;
using Objects.Structural.Materials;
using ETABSv1;

namespace Objects.Converter.ETABS

{
    public partial class ConverterETABS
    {
        public object MaterialToNative(Material material)
        {
            var matType = material.type;
            var eMaterialType = eMatType.Steel; 
            switch (matType)
            {
                case Structural.MaterialType.Steel:
                    eMaterialType = eMatType.Steel;
                    break;
                case Structural.MaterialType.Concrete:
                    eMaterialType= eMatType.Concrete;
                    break;
                case Structural.MaterialType.Other:
                    eMaterialType= eMatType.NoDesign;
                    break;
                case Structural.MaterialType.Aluminium:
                    eMaterialType= eMatType.Aluminum;
                    break;
                case Structural.MaterialType.Rebar:
                    eMaterialType= eMatType.Rebar;
                    break;
                case  Structural.MaterialType.ColdFormed:
                    eMaterialType= eMatType.ColdFormed;
                    break;
                case Structural.MaterialType.Tendon:
                    eMaterialType= eMatType.Tendon;
                    break;
                case  Structural.MaterialType.Masonry:
                    eMaterialType= eMatType.Masonry;
                    break;
            }
            string materialName = material.name;

            //Material Problem 
            if(material.designCode != null)
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

        public Material MaterialToSpeckle(string name)
        {
            var speckleStructMaterial = new Material();
            speckleStructMaterial.name = name;
            eMatType matType = new eMatType();
            int color = 0;
            string notes, GUID;
            notes = GUID = null;
            Model.PropMaterial.GetMaterial(name, ref matType, ref color, ref notes, ref GUID);
            switch (matType)
            {
                case eMatType.Steel:
                    speckleStructMaterial.type = Structural.MaterialType.Steel;
                    break;
                case eMatType.Concrete:
                    speckleStructMaterial.type = Structural.MaterialType.Concrete;
                    break;
                case eMatType.NoDesign:
                    speckleStructMaterial.type = Structural.MaterialType.Other;
                    break;
                case eMatType.Aluminum:
                    speckleStructMaterial.type = Structural.MaterialType.Aluminium;
                    break;
                case eMatType.Rebar:
                    speckleStructMaterial.type = Structural.MaterialType.Rebar;
                    break;
                case eMatType.ColdFormed:
                    speckleStructMaterial.type = Structural.MaterialType.ColdFormed;
                    break;
                case eMatType.Tendon:
                    speckleStructMaterial.type = Structural.MaterialType.Tendon;
                    break;
                case eMatType.Masonry:
                    speckleStructMaterial.type = Structural.MaterialType.Masonry;
                    break;
            }

            return speckleStructMaterial;
        }
    }
}
