using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using System.Linq;


namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object Property1DToNative(Property1D property1D)
        {
            int numbMaterial = 0;
            string[] materials = null;
            Model.PropFrame.GetNameList(ref numbMaterial, ref materials);
            if (!materials.Contains(property1D.material.name)) ;
            {
                MaterialToNative(property1D.material);
            }
            var catalogue = new SectionProfile.Catalogue();
            if (property1D.profile.GetType().Equals(catalogue.GetType()))
            {
                SectionProfile.Catalogue sectionProfile = (SectionProfile.Catalogue)property1D.profile;
                Model.PropFrame.ImportProp(property1D.name, property1D.material.name, sectionProfile.catalogueName + ".xml", sectionProfile.sectionName);
                return property1D.name;
            }
            var rectangle = new SectionProfile.Rectangular();
            if (property1D.profile.GetType().Equals(rectangle.GetType()))
            {
                if(property1D.material.type == Structural.MaterialType.Concrete)
                {
                    SectionProfile.Rectangular sectionProfile = (SectionProfile.Rectangular)property1D.profile;
                    Model.PropFrame.SetRectangle(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width);
                    return property1D.name;
                }
                else
                {
                    SectionProfile.Rectangular sectionProfile = (SectionProfile.Rectangular)property1D.profile;
                    Model.PropFrame.SetTube(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness); 
                    return property1D.name;
                }

            }

            var circular = new SectionProfile.Circular();
            if (property1D.profile.GetType().Equals(circular.GetType()))
            {
                if(property1D.material.type == Structural.MaterialType.Concrete)
                {
                    SectionProfile.Circular sectionProfile = (SectionProfile.Circular)property1D.profile;
                    Model.PropFrame.SetCircle(property1D.name, property1D.material.name, sectionProfile.radius * 2);
                    return property1D.name;
                }
                else
                {
                    SectionProfile.Circular sectionProfile = (SectionProfile.Circular)property1D.profile;
                    Model.PropFrame.SetPipe(property1D.name, property1D.material.name, sectionProfile.radius * 2, sectionProfile.wallThickness);
                    return property1D.name;
                }
            }

            var T = new SectionProfile.Tee();
            if (property1D.profile.GetType().Equals(T.GetType()))
            {
                SectionProfile.Tee sectionProfile = (SectionProfile.Tee)property1D.profile;
                Model.PropFrame.SetConcreteTee(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness, sectionProfile.webThickness, false);
                return property1D.name;
            }

            var I = new SectionProfile.ISection();
            if(property1D.profile.GetType().Equals(I.GetType()))
            {
                SectionProfile.ISection sectionProfile = (SectionProfile.ISection)property1D.profile;
                Model.PropFrame.SetISection(property1D.name, property1D.material.name,sectionProfile.depth,sectionProfile.width,sectionProfile.flangeThickness,sectionProfile.webThickness,sectionProfile.width,sectionProfile.flangeThickness);
                return property1D.name;
            }   

            var Channel = new SectionProfile.Channel();
            if (property1D.profile.GetType().Equals(Channel.GetType()))
            {
                SectionProfile.Channel sectionProfile = (SectionProfile.Channel)property1D.profile;
                Model.PropFrame.SetChannel(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness);
                return property1D.name;
            }

            var Angle = new SectionProfile.Angle();
            if (property1D.profile.GetType().Equals(Channel.GetType()))
            {
                SectionProfile.Angle sectionProfile = (SectionProfile.Angle)property1D.profile;
                Model.PropFrame.SetAngle(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness);
                return property1D.name;
            }

            return null;

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
