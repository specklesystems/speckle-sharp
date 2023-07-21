using System.Collections.Generic;
using Autodesk.Revit.DB;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Defines functionality to retreive <see cref="IRevitCategoryInfo"/> from a <see cref="Base"/> object or a string of the category name.
  /// </summary>
  public interface IAllRevitCategories
  {
    public IRevitCategoryInfo GetRevitCategoryInfo(Base @base);
    public IRevitCategoryInfo GetRevitCategoryInfo(string categoryName);
    public IRevitCategoryInfo UndefinedCategory { get; }
    public IEnumerable<IRevitCategoryInfo> All { get; }
  }
}
