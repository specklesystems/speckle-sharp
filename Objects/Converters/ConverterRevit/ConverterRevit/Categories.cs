using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Objects.Converter.Revit
{
  public static class Categories
  {
    public static readonly List<BuiltInCategory> columnCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns };
    public static readonly List<BuiltInCategory> beamCategories = new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming };
    public static readonly List<BuiltInCategory> ductCategories = new List<BuiltInCategory> { BuiltInCategory.OST_DuctCurves };

    public static bool Contains(this IEnumerable<BuiltInCategory> categories, Category category)
    {
      return categories.Select(x => (int)x).Contains(category.Id.IntegerValue);
    }
  }
}