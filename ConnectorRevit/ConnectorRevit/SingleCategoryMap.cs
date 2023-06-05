#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DesktopUI2.Models.TypeMappingOnReceive;
using Speckle.Core.Models;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace ConnectorRevit
{
  internal class SingleCategoryMap
  {
    private readonly string _category;
    private Dictionary<string, ISingleValueToMap> mappingValues = new(StringComparer.OrdinalIgnoreCase);

    public SingleCategoryMap(string CategoryName)
    {
      _category = CategoryName;
    }
    public SingleCategoryMap(string CategoryName, ICollection<ISingleValueToMap>? mappingValues)
    {
      _category = CategoryName;

      if (mappingValues != null && mappingValues.Count > 0)
      {
        this.mappingValues = mappingValues.ToDictionary(mv => mv.IncomingType, mv => mv);
      }
    }
    public SingleCategoryMap(string CategoryName, Dictionary<string, ISingleValueToMap>? mappingValues)
    {
      _category = CategoryName;

      if (mappingValues != null && mappingValues.Count > 0)
      {
        this.mappingValues = mappingValues;
      }
    }
    public void AddMappingValues(List<ISingleValueToMap> mappingValues)
    {
      foreach (var mappingValue in mappingValues)
      {
        AddMappingValue(mappingValue);
      }
    }

    public void AddMappingValue(ISingleValueToMap mappingValue)
    {
      this.mappingValues[mappingValue.IncomingType] = mappingValue;
    }

    public bool TryGetMappingValue(string type, out ISingleValueToMap singleValueToMap)
    {
      return mappingValues.TryGetValue(type, out singleValueToMap);
    }

    public IEnumerable<ISingleValueToMap> GetMappingValues()
    {
      return mappingValues.Values;
    }
  }
}
