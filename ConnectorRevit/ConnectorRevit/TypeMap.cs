#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Core.Models;
using DesktopUI2.Models.TypeMappingOnReceive;

namespace ConnectorRevit
{
  internal class TypeMapping : Base
  {
    public List<string> Categories = new List<string>();
    private readonly Dictionary<string, SingleCategoryMapping> dataStorage = new();

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

    private SingleCategoryMapping AddCategory(string category, List<MappingValue>? mappingValues = null)
    {
      var newCategory = new SingleCategoryMapping(category, mappingValues);
      dataStorage[category] = newCategory;
      return newCategory;
    }
  }

  internal class SingleCategoryMapping : Base
  {
    private readonly string _category;
    private Dictionary<string, MappingValue> mappingValues = new();
    public SingleCategoryMapping(string CategoryName) 
    { 
      _category = CategoryName;
    }
    public SingleCategoryMapping(string CategoryName, List<MappingValue>? mappingValues) 
    { 
      _category = CategoryName;

      if (mappingValues != null && mappingValues.Count > 0)
      {
        this.mappingValues = mappingValues.ToDictionary(mv => mv.IncomingType, mv => mv);
      }      
    }
    public void AddMappingValues(List<MappingValue> mappingValues, bool overwriteExisting = false)
    {
      foreach (var mappingValue in mappingValues)
      {
        if (this.mappingValues.ContainsKey(mappingValue.IncomingType) && !overwriteExisting)
        {
          continue;
        }

        this.mappingValues[mappingValue.IncomingType] = mappingValue;
      }
    }
  } 
}
