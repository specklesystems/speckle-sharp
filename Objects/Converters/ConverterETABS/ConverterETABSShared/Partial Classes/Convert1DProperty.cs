using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;


namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object Property1DToNative(Property1D property1D)
        {
            SectionProfile.Catalogue sectionProfile = (SectionProfile.Catalogue)property1D.profile;
            Model.PropFrame.ImportProp(property1D.name, property1D.material.name, sectionProfile.catalogueName + ".xml", sectionProfile.sectionName);
            return property1D.name;
        }
        public  Property1D Property1DToSpeckle(string name)
        {
            var speckleStructProperty1D = new Property1D();
            speckleStructProperty1D.name = name;
            string materialProp = null;
            Model.PropFrame.GetMaterial(name, ref materialProp);
            speckleStructProperty1D.material = MaterialToSpeckle(materialProp);
            speckleStructProperty1D.profile = SectionToSpeckle(name);
            return speckleStructProperty1D;
        }

    }
}
