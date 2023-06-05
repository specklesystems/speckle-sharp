#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitElementTypeRetriever<TElementType>
  {
    public string? GetRevitTypeOfBase(Base @base);
    public void SetRevitTypeOfBase(Base @base, string type);
    public string GetRevitCategoryOfBase(Base @base);
    public bool CacheContainsTypeWithName(string baseType);
    public IEnumerable<TElementType> GetAndCacheAvailibleTypes(Base @base);
    public IEnumerable<TElementType> GetAllCachedElementTypes();
  }
}
