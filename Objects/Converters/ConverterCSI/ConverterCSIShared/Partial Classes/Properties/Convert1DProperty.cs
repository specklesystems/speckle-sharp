using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using System.Linq;


namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public object Property1DToNative(Property1D property1D)
    {
      int numbMaterial = 0;
      string[] materials = null;
      Model.PropFrame.GetNameList(ref numbMaterial, ref materials);
      if (property1D.material != null)
      {
        if (!materials.Contains(property1D.material.name))
        {
          MaterialToNative(property1D.material);
        }
      }
      else
      {
        Structural.Materials.Material material = new Structural.Materials.Material("default", Structural.MaterialType.Steel, "Grade 50", "United States", "ASTM A992");
        property1D.material = material;
        MaterialToNative(property1D.material);
      }

      var catalogue = new Catalogue();
      if (property1D.profile.GetType().Equals(catalogue.GetType()))
      {
        Catalogue sectionProfile = (Catalogue)property1D.profile;

        switch (sectionProfile.catalogueName)
        {
          case "CA":
            sectionProfile.catalogueName = "CISC10";
            break;
        }


        Model.PropFrame.ImportProp(property1D.name, property1D.material.name, sectionProfile.catalogueName + ".xml", sectionProfile.sectionName.ToUpper());
        return property1D.name;
      }
      var rectangle = new Rectangular();
      if (property1D.profile.GetType().Equals(rectangle.GetType()))
      {
        if (property1D.material.materialType == Structural.MaterialType.Concrete)
        {
          Rectangular sectionProfile = (Rectangular)property1D.profile;
          Model.PropFrame.SetRectangle(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width);
          return property1D.name;
        }
        else
        {
          Rectangular sectionProfile = (Rectangular)property1D.profile;
          Model.PropFrame.SetTube(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness);
          return property1D.name;
        }

      }

      var circular = new Circular();
      if (property1D.profile.GetType().Equals(circular.GetType()))
      {
        if (property1D.material.materialType == Structural.MaterialType.Concrete)
        {
          Circular sectionProfile = (Circular)property1D.profile;
          Model.PropFrame.SetCircle(property1D.name, property1D.material.name, sectionProfile.radius * 2);
          return property1D.name;
        }
        else
        {
          Circular sectionProfile = (Circular)property1D.profile;
          Model.PropFrame.SetPipe(property1D.name, property1D.material.name, sectionProfile.radius * 2, sectionProfile.wallThickness);
          return property1D.name;
        }
      }

      var T = new Tee();
      if (property1D.profile.GetType().Equals(T.GetType()))
      {
        Tee sectionProfile = (Tee)property1D.profile;
        Model.PropFrame.SetConcreteTee(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness, sectionProfile.webThickness, false);
        return property1D.name;
      }

      var I = new ISection();
      if (property1D.profile.GetType().Equals(I.GetType()))
      {
        ISection sectionProfile = (ISection)property1D.profile;
        Model.PropFrame.SetISection(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness, sectionProfile.width, sectionProfile.flangeThickness);
        return property1D.name;
      }

      var Channel = new Channel();
      if (property1D.profile.GetType().Equals(Channel.GetType()))
      {
        Channel sectionProfile = (Channel)property1D.profile;
        Model.PropFrame.SetChannel(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness);
        return property1D.name;
      }

      var Angle = new Angle();
      if (property1D.profile.GetType().Equals(Channel.GetType()))
      {
        Angle sectionProfile = (Angle)property1D.profile;
        Model.PropFrame.SetAngle(property1D.name, property1D.material.name, sectionProfile.depth, sectionProfile.width, sectionProfile.flangeThickness, sectionProfile.webThickness);
        return property1D.name;
      }

      return null;

    }
    public Property1D Property1DToSpeckle(string name)
    {
      var speckleStructProperty1D = new Property1D();
      speckleStructProperty1D.name = name;
      string materialProp = null;
      Model.PropFrame.GetMaterial(name, ref materialProp);
      speckleStructProperty1D.material = MaterialToSpeckle(materialProp);
      speckleStructProperty1D.profile = SectionToSpeckle(name);

      speckleStructProperty1D.applicationId = $"{speckleStructProperty1D.material.applicationId}:{speckleStructProperty1D.profile}";

      return speckleStructProperty1D;
    }


  }
}