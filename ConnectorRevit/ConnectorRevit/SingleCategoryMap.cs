#nullable enable
using System;
using System.Collections.Generic;
using DesktopUI2.Models.TypeMappingOnReceive;
using Speckle.Newtonsoft.Json;

namespace ConnectorRevit
{
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
      this.mappingValues[mappingValue.IncomingType] = mappingValue;
    }

    public bool TryGetMappingValue(string incomingType, out ISingleValueToMap singleValueToMap)
    {
      return mappingValues.TryGetValue(incomingType, out singleValueToMap);
    }

    public IEnumerable<ISingleValueToMap> GetMappingValues()
    {
      return mappingValues.Values;
    }
  }
}
