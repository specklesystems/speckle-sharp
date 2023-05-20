#nullable enable
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Speckle.Core.Models;

namespace RevitSharedResources.Interfaces
{
  public interface IReceivedObjectsCache
  {
    public Element? GetExistingElementFromApplicationId(Document doc, string applicationId);
    public IEnumerable<Element> GetExistingElementsFromApplicationId(Document doc, string applicationId);
    public void AddReceivedElement(Element element, Base @base);
    public void AddReceivedElements(List<Element> element, Base @base);
    public void RemoveSpeckleId(string applicationId);
    public IEnumerable<string> GetApplicationIds();
  }
}
