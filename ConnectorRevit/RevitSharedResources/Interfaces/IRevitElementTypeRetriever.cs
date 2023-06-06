#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitElementTypeRetriever<TElementType, TBuiltInCategory>
  {
    public string? GetRevitTypeOfBase(Base @base);
    public void SetRevitTypeOfBase(Base @base, string type);
    public bool CacheContainsTypeWithName(string baseType);
    public IEnumerable<TElementType> GetAndCacheAvailibleTypes(IElementTypeInfo<TBuiltInCategory> typeInfo);
    public IEnumerable<TElementType> GetAllCachedElementTypes();
    public IElementTypeInfo<TBuiltInCategory> GetRevitTypeInfo(Base @base);
    public IElementTypeInfo<TBuiltInCategory> GetRevitTypeInfo(string categoryName);
    public IElementTypeInfo<TBuiltInCategory> UndefinedTypeInfo { get; }
  }
}
