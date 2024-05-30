using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.Operations.Send;

namespace Speckle.Connectors.Autocad.HostApp.Extensions;

public static class DocumentExtensions
{
  public static List<AutocadRootObject> GetObjects(this Document doc, IEnumerable<string> objectIds)
  {
    List<AutocadRootObject> objects = new();
    using (TransactionContext.StartTransaction(doc))
    {
      Transaction tr = doc.Database.TransactionManager.TopTransaction;

      foreach (string objectIdHandle in objectIds)
      {
        if (long.TryParse(objectIdHandle, out long parsedId))
        {
          Handle handle = new(parsedId);
          if (doc.Database.TryGetObjectId(handle, out ObjectId myObjectId))
          {
            if (tr.GetObject(myObjectId, OpenMode.ForRead) is DBObject dbObject)
            {
              objects.Add(new(dbObject, objectIdHandle));
            }
          }
        }
      }
    }

    return objects;
  }
}
