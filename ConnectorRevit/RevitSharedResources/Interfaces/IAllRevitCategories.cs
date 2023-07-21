using System.Collections.Generic;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Defines functionality to retreive <see cref="IRevitCategoryInfo"/> from a <see cref="Base"/> object or a string of the category name.
  /// </summary>
  public interface IAllRevitCategories
  {
    public IRevitCategoryInfo GetRevitCategoryInfo<T>(Base @base);
    public IRevitCategoryInfo GetRevitCategoryInfo(Base @base);
    public IRevitCategoryInfo GetRevitCategoryInfo(string categoryName);
  }
}
