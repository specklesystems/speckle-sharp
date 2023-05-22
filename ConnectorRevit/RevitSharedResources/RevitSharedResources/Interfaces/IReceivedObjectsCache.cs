#nullable enable
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// Objects that implement the IReceivedObjectsCache interface are responsible for
  /// reading, querying, mutating, and writing a cache of objects that have been previously received
  /// </summary>
  public interface IReceivedObjectsCache
  {
    public Element? GetExistingElementFromApplicationId(Document doc, string applicationId);
    public IEnumerable<Element> GetExistingElementsFromApplicationId(Document doc, string applicationId);
    public void RemoveSpeckleId(string applicationId);
    public HashSet<string> GetApplicationIds();
    public void AddConvertedElements(IConvertedObjectsCache convertedObjects);
  }
}
