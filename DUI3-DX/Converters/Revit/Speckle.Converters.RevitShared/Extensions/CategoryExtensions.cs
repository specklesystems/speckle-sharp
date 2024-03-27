using Autodesk.Revit.DB;

namespace Speckle.Converters.RevitShared.Extensions;

public static class CategoryExtensions
{
  public static BuiltInCategory GetBuiltInCategory(this Category category)
  {
    return (BuiltInCategory)category.Id.IntegerValue;
  }
}
