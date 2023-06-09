namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Enforces that the object that implements this interface (which should be the converter) has a bridge to <see cref="IAllRevitCategories{TBuiltInCategory}"/>. This allows the connector to gather <see cref="IRevitCategoryInfo{TBuiltInCategory}"/> from the converter without making the converter implement functionality that it isn't really supposed to implement. This is still a violation of the single responcibility principle, but it's a much smaller violation than having the converter implement <see cref="IAllRevitCategories{TBuiltInCategory}"/> directly.
  /// </summary>
  /// <typeparam name="TBuiltInCategory"></typeparam>
  public interface IAllRevitCategoriesExposer<TBuiltInCategory>
  {
    public IAllRevitCategories<TBuiltInCategory> AllCategories { get; }
  }
}
