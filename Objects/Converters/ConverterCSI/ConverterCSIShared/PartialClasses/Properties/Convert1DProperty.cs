#nullable enable
using System;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public bool Property1DExists(string? name)
    {
      string[] properties = Array.Empty<string>();
      int number = 0;

      // TODO: we don't need to call this every time...
      Model.PropFrame.GetNameList(ref number, ref properties);
      if (properties.Contains(name))
      {
        return true;
      }
      return false;
    }

    public string Property1DToNative(Property1D property1D, ApplicationObject appObj)
    {
      if (property1D is null)
        throw new ArgumentNullException(nameof(property1D));

      if (Property1DExists(property1D.name))
      {
        // I don't think we want to update properties
        //throw new ConversionSkippedException($"?????"); //TODO: Asses what to do here!
        // So here, we don't convert because the object already exists.
        // It seems like skip is more appropriate than created tbh... but this seems to
        appObj.Update(status: ApplicationObject.State.Skipped, createdId: property1D.name);
        return property1D.name;
      }

      if (property1D.profile == null)
        throw new ArgumentException("Expected profile to be non-null", nameof(property1D));

      var materialName = MaterialToNative(property1D.material);

      int success;

      if (
        property1D.profile is Catalogue sectionProfile
        && !string.IsNullOrEmpty(sectionProfile.catalogueName)
        && !string.IsNullOrEmpty(sectionProfile.sectionName)
      )
      {
        if (sectionProfile.catalogueName == "CA")
          sectionProfile.catalogueName = "CISC10";

        success = Model.PropFrame.ImportProp(
          property1D.name,
          materialName,
          sectionProfile.catalogueName + ".xml",
          sectionProfile.sectionName.ToUpper()
        );

        if (success != 0)
          throw new ConversionException($"Failed to import a frame section property {property1D.name}");

        return property1D.name;
      }

      success = property1D.profile switch
      {
        Angle o
          => Model.PropFrame.SetAngle(
            property1D.name,
            materialName,
            ScaleToNative(o.depth, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units),
            ScaleToNative(o.webThickness, o.units)
          ),
        Channel o
          => Model.PropFrame.SetChannel(
            property1D.name,
            materialName,
            ScaleToNative(o.depth, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units),
            ScaleToNative(o.webThickness, o.units)
          ),
        Circular { wallThickness: > 0 } o
          => Model.PropFrame.SetPipe(
            property1D.name,
            materialName,
            ScaleToNative(o.radius * 2, o.units),
            ScaleToNative(o.wallThickness, o.units)
          ),
        Circular o => Model.PropFrame.SetCircle(property1D.name, materialName, ScaleToNative(o.radius * 2, o.units)),
        ISection o
          => Model.PropFrame.SetISection(
            property1D.name,
            materialName,
            ScaleToNative(o.depth, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units),
            ScaleToNative(o.webThickness, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units)
          ),
        Rectangular { flangeThickness: > 0, webThickness: > 0 } o
          => Model.PropFrame.SetTube(
            property1D.name,
            materialName,
            ScaleToNative(o.depth, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units),
            ScaleToNative(o.webThickness, o.units)
          ),
        Rectangular o
          => Model.PropFrame.SetRectangle(
            property1D.name,
            materialName,
            ScaleToNative(o.depth, o.units),
            ScaleToNative(o.width, o.units)
          ),
        Tee o
          => Model.PropFrame.SetConcreteTee(
            property1D.name,
            materialName,
            ScaleToNative(o.depth, o.units),
            ScaleToNative(o.width, o.units),
            ScaleToNative(o.flangeThickness, o.units),
            ScaleToNative(o.webThickness, o.units),
            ScaleToNative(o.webThickness, o.units),
            false
          ),
        _
          => throw new ArgumentOutOfRangeException(
            nameof(property1D),
            $"Unsupported profile type {property1D.profile.GetType()}"
          )
      };

      if (success != 0)
        throw new ConversionException($"Failed to create section with profile named {property1D.name}");

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

      speckleStructProperty1D.applicationId =
        $"{speckleStructProperty1D.material.applicationId}:{speckleStructProperty1D.profile.applicationId}";

      return speckleStructProperty1D;
    }
  }
}
