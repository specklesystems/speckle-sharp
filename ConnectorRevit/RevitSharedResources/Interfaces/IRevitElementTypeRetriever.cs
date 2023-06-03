#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IRevitElementTypeRetriever
  {
    public string? GetRevitTypeOfBase(Base @base);
    public string GetRevitCategoryOfBase(Base @base);
    public bool CacheContainsTypeWithName(Base @base, string baseType);
    public IEnumerable<string> GetAllTypeNamesForBase(Base @base);
  }
}
