#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitElementTypeRetriever<TElementType, TBuiltInCategory>
  {
    public string? GetElementType(Base @base);
    public void SetElementType(Base @base, string type);
    public bool CacheContainsTypeWithName(string category, string baseType);
    public IEnumerable<TElementType> GetOrAddAvailibleTypes(IElementTypeInfo<TBuiltInCategory> typeInfo);
    public IEnumerable<TElementType> GetAllCachedElementTypes();
    public void InvalidateElementTypeCache(string categoryName);
  }
}
