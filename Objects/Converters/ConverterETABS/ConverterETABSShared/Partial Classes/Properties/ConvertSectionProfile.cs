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
            int s = Model.PropFrame.GetNameInPropFile(property, ref sectionPropertyName, ref catalogue, ref matProp, ref propType);
            if(s == 0)
            {
                string[] arrayCatPath = catalogue.Split('\\');
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
            }

            double T3 = 0;
            double T2 = 0;
            int color = 0;
            string notes = "";
            string GUID = "";
            string FileName = "";
            s = Model.PropFrame.GetRectangle(property, ref FileName, ref matProp, ref T3, ref T2, ref color, ref notes, ref GUID);

            if (s == 0)
            {
                speckleSectionProfile = new SectionProfile.Rectangular(property,T3,T2);
            }

            double Tf = 0;
            double TwF = 0;
            double Twt = 0;
            bool mirrorAbout3 = false;
            s = Model.PropFrame.GetConcreteTee(property, ref FileName, ref matProp, ref T3, ref T2, ref Tf, ref TwF, ref Twt, ref mirrorAbout3, ref color, ref notes, ref GUID);
            if (s == 0)
            {
                speckleSectionProfile = new SectionProfile.Tee(property,T3,T2,TwF,Tf);
            }

            s = Model.PropFrame.GetCircle(Name, ref FileName, ref matProp, ref T3, ref color, ref notes, ref GUID);
            if (s == 0)
            {
                speckleSectionProfile = new SectionProfile.Circular(property, T3 / 2);
            }
            




            return speckleSectionProfile;
        }
    }
}
