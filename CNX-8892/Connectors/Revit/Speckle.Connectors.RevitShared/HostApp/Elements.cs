using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace Speckle.Connectors.Revit.HostApp;

public static class Elements
{
  public static IEnumerable<Element> GetElements(this Document doc, IEnumerable<string> objectIds)
  {
    return objectIds.Select(doc.GetElement).Where(x => x != null);
  }
}
