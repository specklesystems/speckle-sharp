using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Properties;
using Objects.Structural.Materials;

namespace Objects.Converter.ETABS
{
    class ConvertProperty1D
    {
        public  Property1D Property1DToSpeckle(string name)
        {
            var speckleStructProperty1D = new Property1D();
            speckleStructProperty1D.name = name;
            speckleStructProperty1D.material = null;
            return speckleStructProperty1D;
        }
    }
}
