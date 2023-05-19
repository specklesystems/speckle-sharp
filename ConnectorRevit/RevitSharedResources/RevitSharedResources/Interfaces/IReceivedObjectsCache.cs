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
    public void RemoveSpeckleId(Document doc, string applicationId);
    public void Save();
    public ICollection<string> GetAllApplicationIds(Document doc);
    public IEnumerable<string> GetApplicationIds(Document doc, string streamId);
  }
}
