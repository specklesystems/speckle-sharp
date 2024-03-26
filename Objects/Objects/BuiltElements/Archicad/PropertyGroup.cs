using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

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

  /// <summary>
  /// Turns a List of PropertyGroup into a Base so that it can be used with the Speckle properties prop
  /// </summary>
  /// <param name="propertyGroups"></param>
  /// <returns></returns>
  public static Base? ToBase(List<PropertyGroup>? propertyGroups)
  {
    if (propertyGroups == null || propertyGroups.Count == 0)
    {
      return null;
    }

    var @base = new Base();

    foreach (PropertyGroup propertyGroup in propertyGroups)
    {
      @base[RemoveDisallowedPropNameChars(propertyGroup.name)] = Property.ToBase(propertyGroup.propertyList);
    }

    return @base;
  }
}
