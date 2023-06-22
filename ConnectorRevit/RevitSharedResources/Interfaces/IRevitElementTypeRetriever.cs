#nullable enable
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// <para>This interface defines the functionality related to getting, setting, and caching Revit ElementTypes. This functionality lives in the converter, because getting and setting the type of a specific Base object requires knowledge of the Objects kit.</para> 
  /// The rest of the interface deals with query and caching element types so that both the connector and converter can access the same cache.
  /// <para> TElementType is always the Revit Element type and TBuiltInCategory is always the Revit BuiltInCategory type. These are passed as generic types because the interfaces that are shared between the converter and connector does not have a dependency on the Revit Types (at the moment)</para>
  /// </summary>
  /// <typeparam name="TElementType"></typeparam>
  /// <typeparam name="TBuiltInCategory"></typeparam>
  public interface IRevitElementTypeRetriever
  {
    public string? GetElementType(Base @base);
    public void SetElementType(Base @base, string type);
    public string? GetElementFamily(Base @base);
    public void SetElementFamily(Base @base, string family);
    public bool CacheContainsTypeWithName(string category, string baseType);
    public IEnumerable<ElementType> GetOrAddAvailibleTypes(IRevitCategoryInfo typeInfo);
    public IEnumerable<ElementType> GetAllCachedElementTypes();
    public void InvalidateElementTypeCache(string categoryName);
  }
}
