using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

public static class PropertyHelper
{
  private static readonly HashSet<char> DisallowedPropNameChars = new() { '.', '/' };

  public static string RemoveDisallowedPropNameChars(string propertyName)
  {
    foreach (char c in DisallowedPropNameChars)
    {
      propertyName = propertyName.Replace(c, ' ');
    }

    return propertyName;
  }
}

public class Property : Base
{
  public Property() { }

  [SchemaInfo("Property", "An Archicad element property", "Archicad", "Elements")]
  public Property(string name, object value, [SchemaParamInfo("(Optional) Speckle units.")] string units = "")
  {
    this.name = name;
    this.value = value;
    this.units = units;
  }

  public string name { get; set; }
  public object? value { get; set; }
  public List<object>? values { get; set; }
  public string units { get; set; }
}

public class PropertyGroup : Base
{
  public PropertyGroup() { }

  [SchemaInfo("PropertyGroup", "An Archicad element property group", "Archicad", "Elements")]
  public PropertyGroup(
    string name,
    List<Property> propertyList,
    [SchemaParamInfo("(Optional) Speckle units.")] string units = ""
  )
  {
    this.name = name;
    this.propertyList = propertyList;
    this.units = units;
  }

  public string name { get; set; }
  public List<Property>? propertyList { get; set; }
  public string units { get; set; }
}

public class ComponentProperties : Base
{
  public ComponentProperties() { }

  [SchemaInfo("ComponentProperties", "An Archicad element component properties", "Archicad", "Elements")]
  public ComponentProperties(
    string name,
    List<PropertyGroup> propertyGroups,
    [SchemaParamInfo("(Optional) Speckle units.")] string units = ""
  )
  {
    this.name = name;
    this.propertyGroups = propertyGroups;
    this.units = units;
  }

  public string name { get; set; }
  public List<PropertyGroup>? propertyGroups { get; set; }
  public string units { get; set; }
}

public static class Properties
{
  /// <summary>
  /// Turns a List of Property into a Base so that it can be used with the Speckle properties prop
  /// </summary>
  /// <param name="properties"></param>
  /// <returns></returns>
  public static Base ToBase(this List<Property> properties)
  {
    if (properties == null || properties.Count == 0)
    {
      return null;
    }

    var @base = new Base();

    foreach (Property property in properties)
    {
      var key = PropertyHelper.RemoveDisallowedPropNameChars(property.name);
      if (string.IsNullOrEmpty(key) || @base[key] != null)
      {
        continue;
      }

      @base[key] = property.value;

      // todo
      //property.values;
    }

    return @base;
  }

  /// <summary>
  /// Turns a List of PropertyGroup into a Base so that it can be used with the Speckle properties prop
  /// </summary>
  /// <param name="properties"></param>
  /// <returns></returns>
  public static Base ToBase(this List<PropertyGroup> propertyGroups)
  {
    if (propertyGroups == null || propertyGroups.Count == 0)
    {
      return null;
    }

    var @base = new Base();

    foreach (PropertyGroup propertyGroup in propertyGroups)
    {
      @base[PropertyHelper.RemoveDisallowedPropNameChars(propertyGroup.name)] = Properties.ToBase(
        propertyGroup.propertyList
      );
    }

    return @base;
  }

  /// <summary>
  /// Turns a List of ComponentProperties into a Base so that it can be used with the Speckle properties prop
  /// </summary>
  /// <param name="properties"></param>
  /// <returns></returns>
  public static Base ToBase(this List<ComponentProperties> componentPropertiesList)
  {
    if (componentPropertiesList == null || componentPropertiesList.Count == 0)
    {
      return null;
    }

    var @base = new Base();

    foreach (ComponentProperties componentProperties in componentPropertiesList)
    {
      @base[PropertyHelper.RemoveDisallowedPropNameChars(componentProperties.name)] = Properties.ToBase(
        componentProperties.propertyGroups
      );
    }

    return @base;
  }
}
