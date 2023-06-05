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
