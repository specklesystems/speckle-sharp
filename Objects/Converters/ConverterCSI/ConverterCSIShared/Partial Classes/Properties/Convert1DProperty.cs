using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using System.Linq;
using Speckle.Core.Models;
using Objects.Structural.Geometry;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public ApplicationObject Property1DToNative(Property1D property1D, ref ApplicationObject appObj)
    {
      int numbMaterial = 0;
      string[] materials = new string[] { };
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
        Structural.Materials.StructuralMaterial material = new Structural.Materials.StructuralMaterial("default", Structural.MaterialType.Steel, "Grade 50", "United States", "ASTM A992");
        property1D.material = material;
        MaterialToNative(property1D.material);
      }

      var catalogue = new Catalogue();
      int? success = null;

      if (property1D.profile?.GetType().Equals(catalogue.GetType()) == true)
      {
        Catalogue sectionProfile = (Catalogue)property1D.profile;

        switch (sectionProfile.catalogueName)
        {
          case "CA":
            sectionProfile.catalogueName = "CISC10";
            break;
        }


        success = Model.PropFrame.ImportProp(property1D.name, property1D.material.name, sectionProfile.catalogueName + ".xml", sectionProfile.sectionName.ToUpper());

        if (success == 0)
          appObj.Update(status: ApplicationObject.State.Created, createdId: $"{property1D.name}");
        else
          appObj.Update(status: ApplicationObject.State.Failed);

        return appObj;
      }

      // TODO: these values need to be scaled to native
      switch (property1D.profile)
      {
        case Angle o:
          success = Model.PropFrame.SetAngle(property1D.name, property1D.material.name, o.depth, o.width, o.flangeThickness, o.webThickness);
          break;
        case Channel o:
          success = Model.PropFrame.SetChannel(property1D.name, property1D.material.name, o.depth, o.width, o.flangeThickness, o.webThickness);
          break;
        case Circular o:
          if (o.wallThickness > 0)
            success = Model.PropFrame.SetPipe(property1D.name, property1D.material.name, o.radius * 2, o.wallThickness);
          else
            success = Model.PropFrame.SetCircle(property1D.name, property1D.material.name, o.radius * 2);
          break;
        case ISection o:
          success = Model.PropFrame.SetISection(property1D.name, property1D.material.name, o.depth, o.width, o.flangeThickness, o.webThickness, o.width, o.flangeThickness);
          break;
        case Rectangular o:
          if (o.flangeThickness > 0 && o.webThickness > 0)
            success = Model.PropFrame.SetTube(property1D.name, property1D.material.name, o.depth, o.width, o.flangeThickness, o.webThickness);
          else
            success = Model.PropFrame.SetRectangle(property1D.name, property1D.material.name, o.depth, o.width);
          break;
        case Tee o:
          success = Model.PropFrame.SetConcreteTee(property1D.name, property1D.material.name, o.depth, o.width, o.flangeThickness, o.webThickness, o.webThickness, false);
          break;
      }

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: property1D.name);
      else
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Unable to create section with profile named {property1D.name}");
      return appObj;
    }
    public Property1D Property1DToSpeckle(string name)
    {
      var speckleStructProperty1D = new Property1D();
      speckleStructProperty1D.name = name;
      string materialProp = null;
      Model.PropFrame.GetMaterial(name, ref materialProp);
      speckleStructProperty1D.material = MaterialToSpeckle(materialProp);
      speckleStructProperty1D.profile = SectionToSpeckle(name);

      speckleStructProperty1D.applicationId = $"{speckleStructProperty1D.material.applicationId}:{speckleStructProperty1D.profile.applicationId}";

      return speckleStructProperty1D;
    }


  }
}