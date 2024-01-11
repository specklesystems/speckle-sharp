using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

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

  /// <summary>
  /// Turns a List of ComponentProperties into a Base so that it can be used with the Speckle properties prop
  /// </summary>
  /// <param name="componentPropertiesList"></param>
  /// <returns></returns>
  public static Base? ToBase(List<ComponentProperties>? componentPropertiesList)
  {
    if (componentPropertiesList == null || componentPropertiesList.Count == 0)
    {
      return null;
    }

    var @base = new Base();

    foreach (ComponentProperties componentProperties in componentPropertiesList)
    {
      @base[RemoveDisallowedPropNameChars(componentProperties.name)] = PropertyGroup.ToBase(
        componentProperties.propertyGroups
      );
    }

    return @base;
  }
}
