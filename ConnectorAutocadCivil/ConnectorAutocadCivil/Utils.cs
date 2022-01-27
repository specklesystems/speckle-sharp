using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

using Speckle.Core.Kits;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
#if (CIVIL2021 || CIVIL2022)
using Autodesk.Aec.ApplicationServices;
#endif

namespace Speckle.ConnectorAutocadCivil
{
  public static class Utils
  {

#if AUTOCAD2021
    public static string AutocadAppName = Applications.Autocad2021;
    public static string AppName = "AutoCAD";
#elif AUTOCAD2022
public static string AutocadAppName = Applications.Autocad2022;
    public static string AppName = "AutoCAD";
#elif CIVIL2021
    public static string AutocadAppName = Applications.Civil2021;
    public static string AppName = "Civil 3D";
#elif CIVIL2022
    public static string AutocadAppName = Applications.Civil2022;
    public static string AppName = "Civil 3D";
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
    /// </remarks>
    public static List<string> ToStrings(this ObjectId[] ids) => ids.Select(o => o.ToString().Trim(new char[] { '(', ')' })).ToList();

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
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        foreach (SelectedObject selObj in selection)
        {
          DBObject obj = tr.GetObject(selObj.ObjectId, OpenMode.ForRead);
          if (obj != null && obj.Visible())
            handles.Add(obj.Handle.ToString());
        }
        tr.Commit();
      }

      return handles;
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
      if (tr == null) return ObjectId.Null;

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

    public static BlockTableRecord GetModelSpace(this Database db, OpenMode mode = OpenMode.ForRead)
    {
      return (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(mode);
    }

    /// <summary>
    /// Used to retrieve a DB Object from its handle
    /// </summary>
    /// <param name="handle">Object handle as string</param>
    /// <param name="type">Object class dxf name</param>
    /// <param name="layer">Object layer name</param>
    /// <returns></returns>
    public static DBObject GetObject(this Handle handle, out string type, out string layer)
    {
      Document Doc = Application.DocumentManager.MdiActiveDocument;
      DBObject obj = null;
      type = null;
      layer = null;

      // get objectId
      ObjectId id = Doc.Database.GetObjectId(false, handle, 0);
      if (!id.IsErased && !id.IsNull)
      {
        // get the db object from id
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
      }
      return obj;
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

    /// <summary>
    /// Gets the handles of all visible document objects that can be converted to Speckle
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    public static List<string> ConvertibleObjects(this Document doc, ISpeckleConverter converter)
    {
      var objs = new List<string>();
      using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
      {
        BlockTable blckTbl = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        BlockTableRecord blckTblRcrd = tr.GetObject(blckTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        foreach (ObjectId id in blckTblRcrd)
        {
          DBObject dbObj = tr.GetObject(id, OpenMode.ForRead);
          if (converter.CanConvertToSpeckle(dbObj) && dbObj.Visible())
            objs.Add(dbObj.Handle.ToString());
        }
        tr.Commit();
      }
      return objs;
    }
    #endregion

    /// <summary>
    /// Retrieves the document's units.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static string GetUnits(Document doc)
    {
      var insUnits = doc.Database.Insunits;
      string units = (insUnits == UnitsValue.Undefined) ? Units.None : Units.GetUnitsFromString(insUnits.ToString());
      
#if (CIVIL2021 || CIVIL2022)
      if (units == Units.None)
      {
        // try to get the drawing unit instead
        using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
        {
          var id = DrawingSetupVariables.GetInstance(doc.Database, false);
          var setupVariables = (DrawingSetupVariables)tr.GetObject(id, OpenMode.ForRead);
          var linearUnit = setupVariables.LinearUnit;
          units = Units.GetUnitsFromString(linearUnit.ToString());
          tr.Commit();
        }
      }
#endif
      return units;
    }

  /// <summary>
  /// Retrieves the handle from an input string
  /// </summary>
  /// <param name="str"></param>
  /// <returns></returns>
  public static Handle GetHandle(string str)
    {
      return new Handle(Convert.ToInt64(str, 16));
    }

    /// <summary>
    /// Gets the closes approximate lineweight from double value in mm
    /// </summary>
    /// <param name="weight"></param>
    /// <returns></returns>
    public static LineWeight GetLineWeight(double weight)
    {
      double hundredthMM = weight * 100;
      var weights = Enum.GetValues(typeof(LineWeight)).Cast<int>().ToList();
      int closest = weights.Aggregate((x, y) => Math.Abs(x - hundredthMM) < Math.Abs(y - hundredthMM) ? x : y);
      return (LineWeight)closest;
    }

    /// <summary>
    /// Removes invalid characters for Autocad layer and block names
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string RemoveInvalidChars(string str)
    {
      // using this to handle rhino nested layer syntax
      // replace "::" layer delimiter with "$" (acad standard)
      string cleanDelimiter = str.Replace("::", "$");

      // remove all other invalid chars
      return Regex.Replace(cleanDelimiter, $"[{invalidChars}]", string.Empty);
    }

    public static string RemoveInvalidDynamicPropChars(string str)
    {
      // remove ./
      return Regex.Replace(str, @"[./]", "-");
    }

  }

}
