#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DesktopUI2.Models.TypeMappingOnReceive;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace ConnectorRevit.TypeMapping;

public class TypeMap : ITypeMap
{
  public IEnumerable<string> Categories => categoryToCategoryMap.Keys;

  [JsonProperty]
  private Dictionary<string, SingleCategoryMap> categoryToCategoryMap = new();

  [JsonIgnore]
  private Dictionary<Base, ISingleValueToMap> baseToMappingValue = new();

  [JsonIgnore]
  private HashSet<string> baseIds = new();

  public void AddIncomingType(
    Base @base,
    string incomingType,
    string? incomingFamily,
    string category,
    ISingleHostType initialGuess
  )
  {
    if (baseIds.Contains(@base.id))
    {
      return;
    }

    baseIds.Add(@base.id);

    // add empty category if it isn't already present
    if (!categoryToCategoryMap.TryGetValue(category, out var categoryMappingValues))
    {
      categoryMappingValues = new SingleCategoryMap(category);
      categoryToCategoryMap[category] = categoryMappingValues;
    }

    if (!categoryMappingValues.TryGetMappingValue(incomingFamily, incomingType, out var singleValueToMap))
    {
      singleValueToMap = new RevitMappingValue(incomingType, initialGuess, incomingFamily, true);
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

  public IEnumerable<(Base, ISingleValueToMap)> GetAllBasesWithMappings()
  {
    foreach (var kvp in baseToMappingValue)
    {
      yield return (kvp.Key, kvp.Value);
    }
  }

  public ISingleValueToMap? TryGetMappingValueInCategory(string category, string? incomingFamily, string incomingType)
  {
    if (!categoryToCategoryMap.TryGetValue(category, out var singleCategoryMap))
    {
      return null;
    }

    if (!singleCategoryMap.TryGetMappingValue(incomingFamily, incomingType, out var singleValueToMap))
    {
      return null;
    }

    return singleValueToMap;
  }

  public void AddTypeMap(TypeMap typeMap)
  {
    foreach (var kvp in typeMap.categoryToCategoryMap)
    {
      AddAllMappingValuesInCategory(kvp.Key, kvp.Value);
    }
  }

  private void AddAllMappingValuesInCategory(string category, SingleCategoryMap singleCategoryMap)
  {
    foreach (var mapping in singleCategoryMap.GetMappingValues())
    {
      if (mapping is not RevitMappingValue revitMappingValue)
      {
        throw new ArgumentException(
          $"expected a {nameof(RevitMappingValue)}, but was passed a value of type {mapping.GetType()}"
        );
      }

      // add empty category if it isn't already present
      if (!categoryToCategoryMap.TryGetValue(category, out var categoryMappingValues))
      {
        categoryMappingValues = new SingleCategoryMap(category);
        categoryToCategoryMap[category] = categoryMappingValues;
      }

      if (
        !categoryMappingValues.TryGetMappingValue(
          revitMappingValue.IncomingFamily,
          revitMappingValue.IncomingType,
          out var existingMappingValue
        )
      )
      {
        categoryMappingValues.AddMappingValue(revitMappingValue);
      }
      else
      {
        existingMappingValue.InitialGuess = revitMappingValue.InitialGuess;
        existingMappingValue.MappedHostType = revitMappingValue.MappedHostType;
      }
    }
  }
}
