using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutocadCivilDUI3Shared.Utils;

public static class Objects
{
  public static List<(DBObject obj, string layer, string applicationId)> GetObjectsFromDocument(Document doc, IEnumerable<string> objectIds)
  {
    using DocumentLock acLckDoc = doc.LockDocument();
    var dbObjects = new List<(DBObject, string layer, string applicationId)>();
    using Transaction tr = doc.Database.TransactionManager.StartTransaction();
    foreach (var objectIdHandle in objectIds)
    {
      var handle = new Handle(Convert.ToInt64(objectIdHandle));
      var hasFoundObjectId = doc.Database.TryGetObjectId(handle, out ObjectId myObjectId);
      if (!hasFoundObjectId)
      {
        continue;
      }
      
      var dbObject = tr.GetObject(myObjectId, OpenMode.ForRead);
      if(dbObject == null)
      {
        continue;
      }

      var layer = (dbObject as Entity)?.Layer;
      dbObjects.Add((dbObject, layer, objectIdHandle));
    }
    tr.Commit();
    return dbObjects;
  }
}
