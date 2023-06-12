using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using SCH = RevitSharedResources.Helpers.Categories;

namespace Objects.Converter.Revit
{
  public static class Categories
  {
    /// <summary>
    /// The list of supported BuiltIn category names in Speckle.
    /// This list is auto-generated based on <see cref="RevitCategory"/> enum items.
    /// It represents every item in the enum with an added `OST_` prefix.
    /// </summary>
    public static IReadOnlyList<string> BuiltInCategoryNames = Enum.GetNames(typeof(RevitCategory))
      .Select(c => $"OST_{c}")
      .ToList();

    /// <summary>
    /// Returns the corresponding <see cref="RevitCategory"/> based on a given built-in category name
    /// </summary>
    /// <param name="builtInCategory">The name of the built-in category</param>
    /// <returns>The RevitCategory enum value that corresponds to the given name</returns>
    public static RevitCategory GetSchemaBuilderCategoryFromBuiltIn(string builtInCategory)
    {
      return (RevitCategory)BuiltInCategoryNames.ToList().IndexOf(builtInCategory);
    }

    /// <summary>
    /// Returns the corresponding built-in category name from a specific <see cref="RevitCategory"/>
    /// </summary>
    /// <param name="c">The RevitCategory to convert</param>
    /// <returns>The name of the built-in category that corresponds to the input RevitCategory</returns>
    public static string GetBuiltInFromSchemaBuilderCategory(RevitCategory c)
    {
      return BuiltInCategoryNames[(int)c];
    }
  }
}
