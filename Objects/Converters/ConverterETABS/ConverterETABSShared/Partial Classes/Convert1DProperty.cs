using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Properties;

namespace Objects.Converter.ETABS
{
    class ConvertProperty1D
    {
        public  Property1D Property1DToSpeckle(string name)
        {
            var speckleStructProperty1D = new Property1D();
            speckleStructProperty1D.name = name;
            
            
            return speckleStructProperty1D;
        }
    }
}
