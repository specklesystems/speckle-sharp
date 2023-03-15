using System;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using System.Linq;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public bool Property1DExists(string name)
    {
      string[] properties = new string[] { };
      int number = 0;

      // TODO: we don't need to call this every time...
      Model.PropFrame.GetNameList(ref number, ref properties);
      if (properties.Contains(name))
      {
        return true;
      }
      return false;
    }
    public string Property1DToNative(Property1D property1D, ref ApplicationObject appObj)
    {
      if (property1D == null)
        return null;

      if (Property1DExists(property1D.name))
      {
        // I don't think we want to update properties
        appObj.Update(status: ApplicationObject.State.Skipped, createdId: property1D.name);
        return property1D.name;
      }

      var materialName = MaterialToNative(property1D.material);

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

        success = Model.PropFrame.ImportProp(property1D.name, materialName, sectionProfile.catalogueName + ".xml", sectionProfile.sectionName.ToUpper());

        if (success == 0)
          appObj.Update(status: ApplicationObject.State.Created, createdId: $"{property1D.name}");
        else
          appObj.Update(status: ApplicationObject.State.Failed);

        return property1D.name;
      }

      switch (property1D.profile)
      {
        case Angle o:
          success = Model.PropFrame.SetAngle(
            property1D.name, 
            materialName,
            ScaleToNative(o.depth, o.units), 
            ScaleToNative(o.width, o.units), 
            ScaleToNative(o.flangeThickness, o.units), 
            ScaleToNative(o.webThickness, o.units));
          break;
        case Channel o:
          success = Model.PropFrame.SetChannel(
            property1D.name, 
            materialName, 
            ScaleToNative(o.depth, o.units), 
            ScaleToNative(o.width, o.units), 
            ScaleToNative(o.flangeThickness, o.units), 
            ScaleToNative(o.webThickness, o.units));
          break;
        case Circular o:
          if (o.wallThickness > 0)
            success = Model.PropFrame.SetPipe(
              property1D.name, 
              materialName, 
              ScaleToNative(o.radius * 2, o.units), 
              ScaleToNative(o.wallThickness, o.units));
          else
            success = Model.PropFrame.SetCircle(
              property1D.name, 
              materialName, 
              ScaleToNative(o.radius * 2, o.units));
          break;
        case ISection o:
          success = Model.PropFrame.SetISection(
            property1D.name, 
            materialName,
            ScaleToNative(o.depth, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units),
            ScaleToNative(o.webThickness, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units));
          break;
        case Rectangular o:
          if (o.flangeThickness > 0 && o.webThickness > 0)
            success = Model.PropFrame.SetTube(
              property1D.name,
              materialName,
              ScaleToNative(o.depth, o.units),
              ScaleToNative(o.width, o.units),
              ScaleToNative(o.flangeThickness, o.units),
              ScaleToNative(o.webThickness, o.units));
          else
            success = Model.PropFrame.SetRectangle(
              property1D.name, 
              materialName,
              ScaleToNative(o.depth, o.units), 
              ScaleToNative(o.width, o.units));
          break;
        case Tee o:
          success = Model.PropFrame.SetConcreteTee(
            property1D.name, 
            materialName, 
            ScaleToNative(o.depth, o.units), 
            ScaleToNative(o.width, o.units), 
            ScaleToNative(o.flangeThickness, o.units), 
            ScaleToNative(o.webThickness, o.units), 
            ScaleToNative(o.webThickness, o.units), 
            false);
          break;
      }

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: property1D.name);
      else
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Unable to create section with profile named {property1D.name}");

      return property1D.name;
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
