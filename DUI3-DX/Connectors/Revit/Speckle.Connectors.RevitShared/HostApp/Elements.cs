using Autodesk.Revit.DB;

namespace Speckle.Connectors.Revit.HostApp;

// POC: is this really better than injection? :/
public static class Elements
{
  public static IEnumerable<Element> GetElements(this Document doc, IEnumerable<string> objectIds)
  {
    return objectIds.Select(doc.GetElement).Where(x => x != null);
  }
}
