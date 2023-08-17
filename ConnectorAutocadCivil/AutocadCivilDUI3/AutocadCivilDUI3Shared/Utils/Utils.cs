using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Speckle.Core.Kits;

namespace AutocadCivilDUI3Shared.Utils
{
  public static class Utils
  {
#if AUTOCAD2021DUI3
    public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2021);
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2022DUI3
    public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2022);
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2023DUI3
    public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2023);
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2024DUI3
    public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2024);
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#endif
    public static string invalidChars = @"<>/\:;""?*|=,‘";

    #region extension methods

    /// <summary>
    /// Retrieves object ids as strings
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    /// <remarks>
    /// This is used because for some unfathomable reason, ObjectId.ToString() returns "(id)" instead of "id".
    /// The Handle is a persisitent indentifier which is unique per drawing.
    /// The ObjectId is a non - persitent identifier(reassigned each time the drawing is opened) which is unique per session.
    /// </remarks>
    public static List<string> ToStrings(this ObjectId[] ids) =>
      ids.Select(o => o.Handle.ToString().Trim(new char[] { '(', ')' })).ToList();

    /// <summary>
    /// Retrieves the handle from an input string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool GetHandle(string str, out Handle handle)
    {
      // 
      handle = new Handle();
      try
      {
        long value = Convert.ToInt64(str, 16);
        handle = new Handle(value);
      }
      catch
      {
        return false;
      }
      return true;
    }

    /// <summary>
    /// Used to retrieve a DB Object from its handle
    /// </summary>
    /// <param name="handle">Object handle as string</param>
    /// <param name="type">Object class dxf name</param>
    /// <param name="layer">Object layer name</param>
    /// <returns></returns>
    public static DBObject GetObject(
      this Handle handle,
      Transaction tr,
      out string type,
      out string layer,
      out string applicationId
    )
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;
      DBObject obj = null;
      type = null;
      layer = null;
      applicationId = null;

      // get objectId
      ObjectId id = Doc.Database.GetObjectId(false, handle, 0);
      if (!id.IsErased && !id.IsNull)
      {
        type = id.ObjectClass.DxfName;

        // get the db object from id
        obj = tr.GetObject(id, OpenMode.ForRead);
        if (obj != null)
        {
          Entity objEntity = obj as Entity;
          layer = objEntity.Layer;
          applicationId = ApplicationIdManager.GetFromXData(objEntity);
        }
      }
      return obj;
    }

    /// <summary>
    /// Retrieve handles of visible objects in a selection
    /// </summary>
    /// <param name="selection"></param>
    /// <returns>List of handles as strings</returns>
    /// <remarks>
    /// We are storing obj handles instead of ids because handles persist and are saved with the doc.
    /// ObjectIds are unique to an application session (useful for multi-document operations) but are created with each new session.
    /// </remarks>
    public static List<string> GetHandles(this SelectionSet selection)
    {
      var handles = new List<string>();

      if (selection == null)
        return handles;

      Document Doc = Application.DocumentManager.MdiActiveDocument;
      using (TransactionContext.StartTransaction(Doc))
      {
        Transaction tr = Doc.TransactionManager.TopTransaction;
        foreach (SelectedObject selObj in selection)
        {
          DBObject obj = tr.GetObject(selObj.ObjectId, OpenMode.ForRead);
          if (obj != null && obj.Visible())
          {
            handles.Add(obj.Handle.ToString());
          }
        }
      }

      return handles;
    }

    public static List<string> GetIds(this SelectionSet selection)
    {
      var ids = new List<string>();

      if (selection == null)
        return ids;

      Document Doc = Application.DocumentManager.MdiActiveDocument;
      using (TransactionContext.StartTransaction(Doc))
      {
        Transaction tr = Doc.TransactionManager.TopTransaction;
        foreach (SelectedObject selObj in selection)
        {
          DBObject obj = tr.GetObject(selObj.ObjectId, OpenMode.ForRead);
          if (obj != null && obj.Visible())
          {
            ids.Add(obj.Id.ToString());
          }
        }
      }

      return ids;
    }

    /// <summary>
    /// Adds an entity to the autocad database model space record
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="tr"></param>
    public static ObjectId Append(this Entity entity, string layer = null)
    {
      var db = (entity.Database == null) ? Application.DocumentManager.MdiActiveDocument.Database : entity.Database;
      Transaction tr = db.TransactionManager.TopTransaction;
      if (tr == null)
        return ObjectId.Null;

      BlockTableRecord btr = db.GetModelSpace(OpenMode.ForWrite);
      if (entity.IsNewObject)
      {
        if (layer != null)
          entity.Layer = layer;
        var id = btr.AppendEntity(entity);
        tr.AddNewlyCreatedDBObject(entity, true);
        return id;
      }
      else
      {
        if (layer != null)
          entity.Layer = layer;
        return entity.Id;
      }
    }

    /// <summary>
    /// Gets the document model space
    /// </summary>
    /// <param name="db"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public static BlockTableRecord GetModelSpace(this Database db, OpenMode mode = OpenMode.ForRead)
    {
      return (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(mode);
    }

    /// <summary>
    /// Get visibility of a DBObject
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool Visible(this DBObject obj)
    {
      bool isVisible = true;

      if (obj is Entity)
      {
        Entity ent = obj as Entity;

        if (!ent.Visible)
          return ent.Visible;

        Document Doc = Application.DocumentManager.MdiActiveDocument;
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          LayerTableRecord lyrTblRec = tr.GetObject(ent.LayerId, OpenMode.ForRead) as LayerTableRecord;
          if (lyrTblRec.IsOff)
            isVisible = false;
          tr.Commit();
        }
      }
      else
      {
        PropertyInfo prop = obj.GetType().GetProperty("Visible");
        try
        {
          isVisible = (bool)prop.GetValue(obj);
        }
        catch { }
      }
      return isVisible;
    }

    #endregion
  }
}
