using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DesktopUI2.Models.TypeMappingOnReceive;
using DesktopUI2.ViewModels;

namespace ConnectorRevit.TypeMapping
{
  public class HostTypeContainer : IHostTypeContainer
  {
    private readonly Dictionary<string, HashSet<ISingleHostType>> categoryToTypes = new(StringComparer.OrdinalIgnoreCase);

    public void AddTypesToCategory(string category, IEnumerable<ISingleHostType> newTypes)
    {
      if (!categoryToTypes.TryGetValue(category, out HashSet<ISingleHostType> existingTypes))
      {
        existingTypes = new HashSet<ISingleHostType>();
        categoryToTypes.Add(category, existingTypes);
      }
      foreach (var type in newTypes)
      {
        existingTypes.Add(type);
      }
    }
    public void AddCategoryWithTypesIfCategoryIsNew(string category, IEnumerable<ISingleHostType> types)
    {
      if (categoryToTypes.ContainsKey(category)) return;

      categoryToTypes[category] = types.ToHashSet();
    }

    public ICollection<ISingleHostType> GetTypesInCategory(string category)
    {
      return categoryToTypes[category];
    }

    public ICollection<ISingleHostType> GetAllTypes()
    {
      return categoryToTypes[TypeMappingOnReceiveViewModel.TypeCatMisc];
    }
    public void SetAllTypes(IEnumerable<ISingleHostType> types)
    {
      categoryToTypes[TypeMappingOnReceiveViewModel.TypeCatMisc] = types.ToHashSet();
    }
  }
}
