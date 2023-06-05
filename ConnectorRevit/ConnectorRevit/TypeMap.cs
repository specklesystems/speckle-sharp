#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models.TypeMappingOnReceive;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace ConnectorRevit
{
  public class TypeMap : ITypeMap
  {
    public IEnumerable<string> Categories => categoryToCategoryMap.Keys;
    private readonly Dictionary<string, SingleCategoryMap> categoryToCategoryMap = new();
    private readonly Dictionary<Base, ISingleValueToMap> baseToMappingValue = new();

    public void AddIncomingTypes(Dictionary<string, List<ISingleValueToMap>> mappingValues, out bool newTypesExist)
    {
      newTypesExist = false;
      foreach (var kvp in mappingValues)
      {
        var category = kvp.Key;
        var mappingValueList = kvp.Value;

        if (!categoryToCategoryMap.TryGetValue(category, out var singleCategoryMapping))
        {
          newTypesExist = true;
          singleCategoryMapping = AddCategory(category, mappingValueList);
          continue;
        }

        singleCategoryMapping.AddMappingValues(mappingValueList);
      }
    }

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

    private SingleCategoryMap AddCategory(string category, ICollection<ISingleValueToMap>? mappingValues = null)
    {
      var newCategory = new SingleCategoryMap(category, mappingValues);
      categoryToCategoryMap[category] = newCategory;
      return newCategory;
    }
  }
}
