using System;
using System.Collections.Generic;
using System.Text;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Defines the properties of a single, predefined, category that can be used to group objects that have similar characteristics or filter for objects of that category.
  /// </summary>
  /// <typeparam name="TBuiltInCategory"></typeparam>
  public interface IRevitCategoryInfo<TBuiltInCategory>
  {
    public string CategoryName { get; }
    public Type ElementInstanceType { get; }
    public Type ElementTypeType { get; }
    public List<TBuiltInCategory> BuiltInCategories { get; }
  }
}
