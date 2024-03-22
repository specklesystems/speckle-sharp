using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace AutocadCivilDUI3Shared.Utils;

public static class Objects
{
  public static List<(DBObject obj, string layer, string applicationId)> GetObjectsFromDocument(
    Document doc,
    IEnumerable<string> objectIds
  )
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

      try
      {
        var dbObject = tr.GetObject(myObjectId, OpenMode.ForRead);
        if (dbObject == null)
        {
          continue;
        }

        var layer = (dbObject as Entity)?.Layer;
        dbObjects.Add((dbObject, layer, objectIdHandle));
      }
      catch (Autodesk.AutoCAD.Runtime.Exception e)
      {
        // TODO: think if we need to handle more in here
        if (e.ErrorStatus == ErrorStatus.WasErased)
        {
          continue;
        }
        throw e;
      }
    }
    tr.Commit();
    return dbObjects;
  }
}
