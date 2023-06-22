using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;

namespace RevitSharedResources.Helpers
{
  public struct RevitCategoryInfo : IRevitCategoryInfo
  {
    public RevitCategoryInfo(string name, Type instanceType, Type familyType, List<BuiltInCategory> categories, List<string> categoryAliases = null)
    {
      CategoryName = name;
      ElementInstanceType = instanceType;
      ElementTypeType = familyType;
      BuiltInCategories = categories;
      CategoryAliases = categoryAliases ?? new List<string>();
    }
    public string CategoryName { get; }
    public Type ElementInstanceType { get; }
    public Type ElementTypeType { get; }
    public ICollection<BuiltInCategory> BuiltInCategories { get; }
    public List<string> CategoryAliases { get; }

    public bool ContainsRevitCategory(Category category)
    {
      return BuiltInCategories.Select(x => (int)x).Contains(category.Id.IntegerValue);
    }
    public static bool operator ==(RevitCategoryInfo left, RevitCategoryInfo right) => left.Equals(right);
    public static bool operator !=(RevitCategoryInfo left, RevitCategoryInfo right) => !(left == right);
  }
}
