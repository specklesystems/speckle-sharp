#nullable enable
using System;
using System.Collections.Generic;
using DesktopUI2.Models.TypeMappingOnReceive;
using Speckle.Newtonsoft.Json;

namespace ConnectorRevit.TypeMapping;

internal class SingleCategoryMap
{
  private readonly string _category;

  [JsonProperty]
  private Dictionary<string, ISingleValueToMap> mappingValues = new(StringComparer.OrdinalIgnoreCase);

  public SingleCategoryMap(string CategoryName)
  {
    _category = CategoryName;
  }

  public void AddMappingValue(ISingleValueToMap mappingValue)
  {
    if (mappingValue is not RevitMappingValue revitMappingValue)
    {
      throw new ArgumentException(
        $"the {nameof(AddMappingValue)} function is expecting to be passed a {nameof(RevitMappingValue)}, but was passed a value of type {mappingValue.GetType()}"
      );
    }
    this.mappingValues[UniqueTypeName(revitMappingValue.IncomingFamily, revitMappingValue.IncomingType)] = mappingValue;
  }

  public bool TryGetMappingValue(string? incomingFamily, string incomingType, out ISingleValueToMap singleValueToMap)
  {
    return mappingValues.TryGetValue(UniqueTypeName(incomingFamily, incomingType), out singleValueToMap);
  }

  public IEnumerable<ISingleValueToMap> GetMappingValues()
  {
    return mappingValues.Values;
  }

  private static string UniqueTypeName(string? family, string type)
  {
    return $"{family}{type}";
  }
}
