using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DesktopUI2.Models.TypeMappingOnReceive;
using DesktopUI2.ViewModels;

namespace ConnectorRevit
{
  internal class HostTypeAsStringContainer : IHostTypeAsStringContainer
  {
    private readonly Dictionary<string, HashSet<string>> categoryToTypes = new(StringComparer.OrdinalIgnoreCase);

    public void AddTypesToCategory(string category, IEnumerable<string> newTypes)
    {
      if (!categoryToTypes.TryGetValue(category, out HashSet<string> existingTypes))
      {
        existingTypes = new HashSet<string>();
        categoryToTypes.Add(category, existingTypes);
      }
      foreach (var type in newTypes)
      {
        existingTypes.Add(type);
      }
    }
    public void AddCategoryWithTypesIfCategoryIsNew(string category, IEnumerable<string> types) 
    {
      if (categoryToTypes.ContainsKey(category)) return;

      categoryToTypes[category] = types.ToHashSet();
    }

    public ICollection<string> GetTypesInCategory(string category)
    {
      return categoryToTypes[category];
    }

    public ICollection<string> GetAllTypes()
    {
      return categoryToTypes[TypeMappingOnReceiveViewModel.TypeCatMisc];
    }
    public void SetAllTypes(IEnumerable<string> types)
    {
      categoryToTypes[TypeMappingOnReceiveViewModel.TypeCatMisc] = types.ToHashSet();
    }
  }
}
