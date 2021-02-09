using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Kits;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace Speckle.ConnectorAutocadCivil
{
  public static class Utils
  {

#if AUTOCAD2021
    public static string AutocadAppName = Applications.Autocad2021;
#elif CIVIL2021
    public static string AutocadAppName = Applications.Civil2021;
#endif

    #region extension methods

    /// <summary>
    /// Retrieves object ids as strings
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    /// <remarks>
    /// This is used because for some unfathomable reason, ObjectId.ToString() returns "(id)" instead of "id".
    /// </remarks>
    public static List<string> ToStrings(this ObjectId[] ids) => ids.Select(o => o.ToString().Trim(new char[] { '(', ')' })).ToList();

    /// <summary>
    /// Retrieve selection object handles
    /// </summary>
    /// <param name="selection"></param>
    /// <returns>List of handles as strings</returns>
    /// <remarks>
    /// We are storing obj handles instead of ids because handles persist and are saved with the doc.
    /// ObjectIds are unique to an application session (useful for multi-document operations) but are created with each new session.
    /// </remarks>
    public static List<string> GetHandles(this SelectionSet selection)
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;
      var handles = new List<string>();
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        foreach (SelectedObject obj in selection)
        {
          if (obj != null)
          {
            Entity objEntity = tr.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
            if (objEntity != null)
              handles.Add(objEntity.Handle.ToString());
          }
        }
        tr.Commit();
      }
      return handles;
    }

    /// <summary>
    /// Adds an entity to the autocad database block table
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="tr"></param>
    public static void Append(this Entity entity, string layer, Transaction tr)
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;

      // open blocktable record for editing
      BlockTableRecord btr = (BlockTableRecord)tr.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite);

      entity.Layer = layer;
      btr.AppendEntity(entity);
      tr.AddNewlyCreatedDBObject(entity, true);
    }

    /// <summary>
    /// Used to retrieve DB Object from its handle
    /// </summary>
    /// <param name="handle">Object handle as string</param>
    /// <param name="type">Object class dxf name</param>
    /// <param name="layer">Object layer name</param>
    /// <returns></returns>
    public static DBObject GetObject(this Handle handle, out string type, out string layer)
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;

      // get objectId
      ObjectId id = Doc.Database.GetObjectId(false, handle, 0);

      // get the db object from id
      DBObject obj = null;
      type = null;
      layer = null;
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        obj = tr.GetObject(id, OpenMode.ForRead);
        if (obj != null)
        {
          Entity objEntity = obj as Entity;
          type = id.ObjectClass.DxfName;
          layer = objEntity.Layer;
        }
        tr.Commit();
      }

      return obj;
    }
    #endregion
  }
  
}
