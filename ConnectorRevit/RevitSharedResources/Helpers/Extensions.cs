using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Helpers.Extensions;

/// <summary>
/// Contains any extension methods related to Revit elements.
/// </summary>
public static class Extensions
{
  /// <summary>
  /// Checks if an element has a physical representation in the model.
  /// An element is considered a "PhysicalElement" when its category type is of type <see cref="CategoryType.Model"/>
  /// and it's category is supported.
  /// </summary>
  /// <param name="e">The Element to check for</param>
  /// <returns>True if the element has physical representation, false otherwise</returns>
  public static bool IsPhysicalElement(this Element e)
  {
    if (e.Category == null)
    {
      return false;
    }

    if (e.ViewSpecific)
    {
      return false;
    }
    // TODO: Should this be filtering using the Supported categories list instead?
    // exclude specific unwanted categories
    if (((BuiltInCategory)e.Category.Id.IntegerValue) == BuiltInCategory.OST_HVAC_Zones)
    {
      return false;
    }

    return e.Category.CategoryType == CategoryType.Model && e.Category.CanAddSubcategory;
  }

  /// <summary>
  /// Checks if a specific category is contained within a given enumerable of categories.
  /// </summary>
  /// <param name="categories">The enumerable categories to search through</param>
  /// <param name="category">The category to search for</param>
  /// <returns>True if category exists, false otherwise</returns>
  /// <remarks>This function will never throw, returning false instead</remarks>
  public static bool HasCategory(this IEnumerable<BuiltInCategory> categories, Category category)
  {
    if (category?.Id?.IntegerValue is not int categoryInt)
    {
      return false;
    }

    return categories.Select(x => (int)x).Contains(categoryInt);
  }

  /// <summary>
  /// Checks if a category is supported when sending
  /// See: https://docs.google.com/spreadsheets/d/1By5RM0PCMw-M1ZVubXD3bF1FVz3Uk4u4vBrRUhJzWXw/edit?usp=sharing
  /// </summary>
  /// <param name="category">The category to check support for</param>
  /// <returns>True if the CategoryType is Model, AnalyticalModel or Internal</returns>
  public static bool IsCategorySupported(this Category category)
  {
    if (
      category.CategoryType == CategoryType.Model
      || category.CategoryType == CategoryType.AnalyticalModel
      || category.CategoryType == CategoryType.Internal
      || category.Id.IntegerValue == -2000220
    ) // Grids
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Checks if an element's category is supported for conversion
  /// </summary>
  /// <param name="e">The element to check support for</param>
  /// <returns>True if the element's category is supported and if the element is not view dependent</returns>
  public static bool IsElementSupported(this Element e)
  {
    if (e.Category == null || e.ViewSpecific || !IsCategorySupported(e.Category))
    {
      return false;
    }

    return true;
  }
}
