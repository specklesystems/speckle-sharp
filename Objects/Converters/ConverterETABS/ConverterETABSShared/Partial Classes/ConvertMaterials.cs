using System;
using System.Collections.Generic;
using Objects.Structural.Materials;
using ETABSv1;

namespace Objects.Converter.ETABS

{
    public partial class ConverterETABS
    {
        public Material MaterialToSpeckle(string name)
        {
            var speckleStructMaterial = new Material();
            speckleStructMaterial.name = name;
            eMatType matType = new eMatType();
            int color = 0;
            string notes, GUID;
            notes = GUID = null;
            Doc.Document.PropMaterial.GetMaterial(name, ref matType, ref color, ref notes, ref GUID);
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
