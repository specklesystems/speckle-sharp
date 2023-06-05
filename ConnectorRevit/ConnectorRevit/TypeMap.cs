#nullable enable
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models.TypeMappingOnReceive;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace ConnectorRevit
{
  public class TypeMap : ITypeMap
  {
    public IEnumerable<string> Categories => categoryToCategoryMap.Keys;
    [JsonProperty]
    private Dictionary<string, SingleCategoryMap> categoryToCategoryMap = new();
    [JsonIgnore]
    private Dictionary<Base, ISingleValueToMap> baseToMappingValue = new();

    //public TypeMap(Dictionary<Base, MappingValue> baseToMappingValue)
    //{
    //  this.baseToMappingValue = baseToMappingValue;
    //}

    public void AddIncomingType(Base @base, string incomingType, string category, string initialGuess, out bool isNewType, bool overwriteExisting = false)
    {
      isNewType = false;

      // add empty category if it isn't already present
      if (!categoryToCategoryMap.TryGetValue(category, out var categoryMappingValues))
      {
        categoryMappingValues = new SingleCategoryMap(category);
        categoryToCategoryMap[category] = categoryMappingValues;
      }

      if (!categoryMappingValues.TryGetMappingValue(incomingType, out var singleValueToMap) || overwriteExisting)
      {
        isNewType = true;
        singleValueToMap = new MappingValue(incomingType, initialGuess, true);
        categoryMappingValues.AddMappingValue(singleValueToMap);
      }

      baseToMappingValue.Add(@base, singleValueToMap);
    }

    public bool HasCategory(string category)
    {
      return categoryToCategoryMap.ContainsKey(category);
    }

    public IEnumerable<ISingleValueToMap> GetValuesToMapOfCategory(string category)
    {
      if (!categoryToCategoryMap.TryGetValue(category, out var singleCategoryMap))
      {
        return Enumerable.Empty<ISingleValueToMap>();
      }

      return singleCategoryMap.GetMappingValues();
    }

    public IEnumerable<(Base,ISingleValueToMap)> GetAllBasesWithMappings()
    {
      foreach (var kvp in baseToMappingValue)
      {
        yield return (kvp.Key, kvp.Value);
      }
    }

    public ISingleValueToMap? TryGetMappingValueInCategory(string category, string incomingType)
    {
      if (!categoryToCategoryMap.TryGetValue(category, out var singleCategoryMap))
      {
        return null;
      }

      if (!singleCategoryMap.TryGetMappingValue(incomingType, out var singleValueToMap)) 
      {
        return null;
      }

      return singleValueToMap;
    }
  }
}
