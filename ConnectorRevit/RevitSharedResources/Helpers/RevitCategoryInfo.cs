using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitSharedResources.Interfaces;

namespace RevitSharedResources.Helpers
{
  public class RevitCategoryInfo : IRevitCategoryInfo
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

    public List<ElementType> GetElementTypes(Document document)
    {
      return GetElementTypes<ElementType>(document);
    }
    public List<T> GetElementTypes<T>(Document document)
      where T : ElementType
    {
      var collector = new FilteredElementCollector(document);
      if (BuiltInCategories.Count > 0)
      {
        using var filter = new ElementMulticategoryFilter(BuiltInCategories);
        collector = collector.WherePasses(filter);
      }
      if (ElementTypeType != null)
      {
        collector = collector.OfClass(ElementTypeType);
      }
      var elementTypes = collector.WhereElementIsElementType().Cast<T>().ToList();
      collector.Dispose();
      return elementTypes;
    }

    public string GetCategorySpecificTypeName(string typeName)
    {
      return CategoryName + "_" + typeName;
    }
  }
}
