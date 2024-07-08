using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
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
          // Note: Fatal crash happens here when objects are deleted, so we need to catch it.
          try
          {
            if (doc.Database.TryGetObjectId(handle, out ObjectId myObjectId))
            {
              if (tr.GetObject(myObjectId, OpenMode.ForRead) is DBObject dbObject)
              {
                objects.Add(new(dbObject, objectIdHandle));
              }
            }
          }
          catch (Autodesk.AutoCAD.Runtime.Exception e)
          {
            if (e.ErrorStatus == ErrorStatus.WasErased) // Note: TBD if we want to catch more things in here. For now maybe not, but it does seem like this function gets into "crashes the host app territory"
            {
              continue;
            }

            throw;
          }
        }
      }
    }

    return objects;
  }
}
