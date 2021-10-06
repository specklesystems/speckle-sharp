using System;
using System.Collections.Generic;
using Objects.Structural.Properties.Profiles;
using ETABSv1;
using System.Linq;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public SectionProfile SectionToSpeckle(string property)
        {
            var speckleSectionProfile = new SectionProfile();
            eFramePropType propType = eFramePropType.I;
            string catalogue = "";
            string matProp = "";
            string sectionPropertyName = "";
            Model.PropFrame.GetNameInPropFile(property, ref sectionPropertyName, ref catalogue, ref matProp, ref propType);
            string[] arrayCatPath =  catalogue.Split('\\');
            catalogue = arrayCatPath.Last();
            arrayCatPath = catalogue.Split('.');
            catalogue = arrayCatPath[0];
            switch (propType)
            {
                case eFramePropType.SD:
                    speckleSectionProfile = new SectionProfile.Explicit();
                    break;
                default:
                    speckleSectionProfile = new SectionProfile.Catalogue(property, catalogue, propType.ToString(), sectionPropertyName);
                    break;
            }


            return speckleSectionProfile;
        }
    }
}
