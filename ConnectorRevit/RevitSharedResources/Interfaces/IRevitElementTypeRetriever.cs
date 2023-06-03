#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitElementTypeRetriever<ElementType>
  {
    public string? GetRevitTypeOfBase(Base @base);
    public string GetRevitCategoryOfBase(Base @base);
    public bool CacheContainsTypeWithName(string baseType);
    public IEnumerable<string> GetAllTypeNamesForBase(Base @base);
    public IEnumerable<ElementType> GetAndCacheAvailibleTypes(Base @base);
  }
}
