using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Defines functionality to retreive <see cref="IRevitCategoryInfo{TBuiltInCategory}"/> from a <see cref="Base"/> object or a string of the category name.
  /// </summary>
  /// <typeparam name="TBuiltInCategory"></typeparam>
  public interface IAllRevitCategories<TBuiltInCategory>
  {
    public IRevitCategoryInfo<TBuiltInCategory> GetRevitCategoryInfo(Base @base);
    public IRevitCategoryInfo<TBuiltInCategory> GetRevitCategoryInfo(string categoryName);
    public IRevitCategoryInfo<TBuiltInCategory> UndefinedCategory { get; }
  }
}
