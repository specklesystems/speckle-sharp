#nullable enable
using System;
using System.Collections.Generic;
using Objects.Structural.Properties;
using Objects.Structural.Properties.Profiles;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Objects.Converter.CSI;

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

  public string? TryConvertProperty1DToNative(Property1D? property1D, IList<string>? parentLog)
  {
    if (property1D is null)
    {
      return null;
    }

    try
    {
      return Property1DToNative(property1D);
    }
    catch (ConversionNotSupportedException ex)
    {
      parentLog?.Add(ex.Message);
      return property1D.name;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Failed to convert property {propertyName}", property1D.name);
      parentLog?.Add($"Failed to convert property {property1D.name}: {ex.Message}");
      return null;
    }
  }

  public string Property1DToNative(Property1D property1D)
  {
    if (property1D is null)
    {
      throw new ArgumentNullException(nameof(property1D));
    }

    if (Property1DExists(property1D.name))
    {
      throw new ConversionNotSupportedException(
        $"Property {property1D.name} was not updated because it already exists"
      );
    }

    var materialName = MaterialToNative(property1D.material);

    int success;

    if (
      property1D.profile is Catalogue sectionProfile
      && !string.IsNullOrEmpty(sectionProfile.catalogueName)
      && !string.IsNullOrEmpty(sectionProfile.sectionName)
    )
    {
      if (sectionProfile.catalogueName == "CA")
      {
        sectionProfile.catalogueName = "CISC10";
      }

      success = Model.PropFrame.ImportProp(
        property1D.name,
        materialName,
        sectionProfile.catalogueName + ".xml",
        sectionProfile.sectionName.ToUpper()
      );

      if (success != 0)
      {
        throw new ConversionException($"Failed to import a frame section property {property1D.name}");
      }

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
      _ => throw new ConversionNotSupportedException($"Unsupported profile type {property1D.profile.GetType()}")
    };

    if (success != 0)
    {
      throw new ConversionException($"Failed to create section with profile named {property1D.name}");
    }

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
