using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

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

  /// <summary>
  /// Turns a List of Property into a Base so that it can be used with the Speckle properties prop
  /// </summary>
  /// <param name="properties"></param>
  /// <returns></returns>
  public static Base? ToBase(List<Property>? properties)
  {
    if (properties == null || properties.Count == 0)
    {
      return null;
    }

    var @base = new Base();

    foreach (Property property in properties)
    {
      var key = RemoveDisallowedPropNameChars(property.name);
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
}
