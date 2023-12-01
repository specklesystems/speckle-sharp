using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutocadCivilDUI3Shared.Utils;

public static class Objects
{
  public static List<DBObject> GetObjectsFromDocument(Document doc, IEnumerable<string> objectIds)
  {
    using DocumentLock acLckDoc = doc.LockDocument();
    using Transaction tr = doc.Database.TransactionManager.StartTransaction();
    List<DBObject> dbObjects = objectIds.Select(objectId => GetObjectFromDocument(tr, objectId)).ToList();
    tr.Commit();
    return dbObjects;
  }

  private static DBObject GetObjectFromDocument(Transaction tr, string objectId)
  {
    Utils.GetHandle(objectId, out Handle hn);
    return hn.GetObject(tr, out string _, out string _, out string _);
  }
}
