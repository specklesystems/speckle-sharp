using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IElementTypeInfoExposer<TBuiltInCategory>
  {
    public IElementTypeInfo<TBuiltInCategory> GetRevitTypeInfo(Base @base);
    public IElementTypeInfo<TBuiltInCategory> GetRevitTypeInfo(string categoryName);
    public IElementTypeInfo<TBuiltInCategory> UndefinedTypeInfo { get; }
  }
}
