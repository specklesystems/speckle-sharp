using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace Speckle.ConnectorRevitDUI3.Utils;

public static class Objects
{
  public static List<Element> GetObjectsFromDocument(Document doc, IEnumerable<string> objectIds)
  {
    return objectIds
      .Select(doc.GetElement)
      .Where(x => x != null).ToList();
  }
}
