#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public class TypeMap : ITypeMap
  {
    public IEnumerable<string> Categories => dataStorage.Keys;
    private readonly Dictionary<string, SingleCategoryMap> dataStorage = new();

    public void AddIncomingTypes(Dictionary<string, List<MappingValue>> mappingValues, out bool newTypesExist)
    {
      newTypesExist = false;
      foreach (var kvp in mappingValues)
      {
        var category = kvp.Key;
        var mappingValueList = kvp.Value;

        if (!dataStorage.TryGetValue(category, out var singleCategoryMapping))
        {
          newTypesExist = true;
          singleCategoryMapping = AddCategory(category);
          continue;
        }

        singleCategoryMapping.AddMappingValues(mappingValueList);
      }
    }

    public bool HasCategory(string category)
    {
      return dataStorage.ContainsKey(category);
    }

    public IEnumerable<ISingleValueToMap> GetValuesToMapOfCategory()
    {

    }

    private SingleCategoryMap AddCategory(string category, List<MappingValue>? mappingValues = null)
    {
      var newCategory = new SingleCategoryMap(category, mappingValues);
      dataStorage[category] = newCategory;
      return newCategory;
    }

    private SingleCategoryMap? GetCategory(string category)
    {
      if (dataStorage.TryGetValue(category, out var categoryMap))
      {
        return categoryMap;
      }
      return null;
    }
  }
}
