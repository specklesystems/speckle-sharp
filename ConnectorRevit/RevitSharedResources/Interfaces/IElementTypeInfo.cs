using System;
using System.Collections.Generic;
using System.Text;

namespace RevitSharedResources.Interfaces
{
  public interface IElementTypeInfo<TBuiltInCategory>
  {
    public string CategoryName { get; }
    public Type ElementInstanceType { get; }
    public Type ElementTypeType { get; }
    public List<TBuiltInCategory> BuiltInCategories { get; }
  }
}
