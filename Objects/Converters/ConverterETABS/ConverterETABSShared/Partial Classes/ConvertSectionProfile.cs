using System;
using System.Collections.Generic;
using Objects.Structural.Properties.Profiles;
using ETABSv1;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public SectionProfile SectionToSpeckle(string name,string property)
        {
            var speckleSectionProfile = new SectionProfile();
            eFramePropType propType = eFramePropType.I;
            Model.PropFrame.GetTypeOAPI(name, ref propType);
            switch (propType)
            {
                case eFramePropType.SD:
                    speckleSectionProfile = new SectionProfile.Explicit();
                    break;
                default:
                    speckleSectionProfile = new SectionProfile.Catalogue(name, "default", propType.ToString(), property);
                    break;
            }

            return speckleSectionProfile;
        }
    }
}
