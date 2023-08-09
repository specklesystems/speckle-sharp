using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Defines the properties of a single, predefined, category that can be used to group objects that have similar characteristics or filter for objects of that category.
  /// </summary>
  public interface IRevitCategoryInfo
  {
    public string CategoryName { get; }
    public List<string> CategoryAliases { get; }
    public Type ElementInstanceType { get; }
    public Type ElementTypeType { get; }
    public ICollection<BuiltInCategory> BuiltInCategories { get; }
    public bool ContainsRevitCategory(Category category);
    List<ElementType> GetElementTypes(Document document);
    List<T> GetElementTypes<T>(Document document)
      where T : ElementType;
    string GetCategorySpecificTypeName(string typeName);
  }
}
