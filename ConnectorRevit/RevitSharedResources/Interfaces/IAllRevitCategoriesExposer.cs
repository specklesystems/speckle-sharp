namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Enforces that the object that implements this interface (which should be the converter) has a bridge to <see cref="IAllRevitCategories"/>. This allows the connector to gather <see cref="IRevitCategoryInfo"/> from the converter without making the converter implement functionality that it isn't really supposed to implement. This is still a violation of the single responcibility principle, but it's a much smaller violation than having the converter implement <see cref="IAllRevitCategories"/> directly.
  /// </summary>
  public interface IAllRevitCategoriesExposer
  {
    public IAllRevitCategories AllCategories { get; }
  }
}
