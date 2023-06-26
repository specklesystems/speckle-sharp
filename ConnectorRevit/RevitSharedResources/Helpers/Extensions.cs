using System;
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
      return false;
    if (e.ViewSpecific)
      return false;
    // TODO: Should this be filtering using the Supported categories list instead?
    // exclude specific unwanted categories
    if (((BuiltInCategory)e.Category.Id.IntegerValue) == BuiltInCategory.OST_HVAC_Zones)
      return false;
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
    try
    {
      return categories.Select(x => (int)x).Contains(category.Id.IntegerValue);
    }
    catch (Exception e)
    {
      return false;
    }
  }

  /// <summary>
  /// Checks if an element's category is supported for conversion
  /// </summary>
  /// <param name="e">The element to check support for</param>
  /// <returns>True if the element's category is contained in <see cref="Categories.SupportedBuiltInCategories"/>, false otherwise.</returns>
  public static bool IsElementSupported(this Element e)
  {
    if (e.Category == null)
      return false;
    if (e.ViewSpecific)
      return false;

    if (Categories.SupportedBuiltInCategories.Contains((BuiltInCategory)e.Category.Id.IntegerValue))
      return true;
    return false;
  }
}
