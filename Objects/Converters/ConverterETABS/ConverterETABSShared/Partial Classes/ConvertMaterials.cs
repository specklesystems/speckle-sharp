using System;
using System.Collections.Generic;
using Objects.Structural.Materials;

namespace Objects.Converter.ETABS

{
    public partial class ConverterETABS
    {
        public Material MaterialToSpeckle(string name)
        {
            var speckleStructMaterial = new Material();
            speckleStructMaterial.name = name;
            speckleStructMaterial.type = Structural.MaterialType.Aluminium;
            return speckleStructMaterial;
        }
    }
}
