using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Defines the properties of a single, predefined, category that can be used to group objects that have similar characteristics or filter for objects of that category.
  /// </summary>
  public interface IRevitCategoryInfo
  {
    public string CategoryName { get; }
    public Type ElementInstanceType { get; }
    public Type ElementTypeType { get; }
    public ICollection<BuiltInCategory> BuiltInCategories { get; }
  }
}
