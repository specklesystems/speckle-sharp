using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace Speckle.Connectors.Revit.HostApp;

public static class Elements
{
  public static List<Element> GetElementsFromDocument(Document doc, IEnumerable<string> objectIds) =>
    objectIds.Select(doc.GetElement).Where(x => x != null).ToList();
}
