using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace AutocadCivilDUI3Shared.Utils
{
  public static class Utils
  {
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
