#nullable enable
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// <para>This interface defines the functionality related to getting and setting the "type" and "family" values on Base objects. This functionality lives in the converter, because getting and setting the type of a specific Base object requires knowledge of the Objects kit.</para> 
  /// </summary>
  public interface IRevitElementTypeRetriever
  {
    public string? GetElementType(Base @base);
    public void SetElementType(Base @base, string type);
    public string? GetElementFamily(Base @base);
    public void SetElementFamily(Base @base, string family);
  }
}
