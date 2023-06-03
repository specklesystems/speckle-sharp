using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  internal class SingleCategoryMap
  {
    private readonly string _category;
    private Dictionary<string, MappingValue> mappingValues = new();
    public SingleCategoryMap(string CategoryName)
    {
      _category = CategoryName;
    }
    public SingleCategoryMap(string CategoryName, List<MappingValue>? mappingValues)
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
