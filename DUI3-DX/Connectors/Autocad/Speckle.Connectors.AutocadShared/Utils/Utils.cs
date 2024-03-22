using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.HostApp;

namespace Speckle.Connectors.Autocad.Utils;

public static class Utils
{
  public static List<(DBObject, string)> GetObjects(this Document doc, IEnumerable<string> objectIds)
  {
    List<(DBObject, string)> objects = new();
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
              objects.Add((dbObject, objectIdHandle));
            }
          }
        }
      }
    }

    return objects;
  }
}
