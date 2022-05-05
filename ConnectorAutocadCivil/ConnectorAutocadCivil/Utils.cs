using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

using Speckle.Core.Kits;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using Speckle.Core.Models;
#if (CIVIL2021 || CIVIL2022)
using Autodesk.Aec.ApplicationServices;
using Autodesk.Aec.PropertyData.DatabaseServices;
#endif

namespace Speckle.ConnectorAutocadCivil
{
  public static class Utils
  {

#if AUTOCAD2021
    public static string VersionedAppName = VersionedHostApplications.Autocad2021;
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2022
    public static string VersionedAppName = VersionedHostApplications.Autocad2022;
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif CIVIL2021
    public static string VersionedAppName = VersionedHostApplications.Civil2021;
    public static string AppName = HostApplications.Civil.Name;
    public static string Slug = HostApplications.Civil.Slug;
#elif CIVIL2022
    public static string VersionedAppName = VersionedHostApplications.Civil2022;
    public static string AppName = HostApplications.Civil.Name;
    public static string Slug = HostApplications.Civil.Slug;
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
    public static DBObject GetObject(this Handle handle, Transaction tr, out string type, out string layer)
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
        obj = tr.GetObject(id, OpenMode.ForRead);
        if (obj != null)
        {
          Entity objEntity = obj as Entity;
          type = id.ObjectClass.DxfName;
          layer = objEntity.Layer;
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

#if CIVIL2021 || CIVIL2022
    /// <summary>
    /// Get the property sets of  DBObject
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static List<Dictionary<string, object>> GetPropertySets(this DBObject obj, Transaction tr)
    {
      var sets = new List<Dictionary<string, object>>();
      ObjectIdCollection propertySets = null;
      try
      {
        propertySets = PropertyDataServices.GetPropertySets(obj);
      }
      catch (Exception e) 
      { }
      if (propertySets == null) return sets;

      foreach (ObjectId id in propertySets)
      {
        var setDictionary = new Dictionary<string, object>();

        PropertySet propertySet = (PropertySet)tr.GetObject(id, OpenMode.ForRead);
        PropertySetDefinition setDef = (PropertySetDefinition)tr.GetObject(propertySet.PropertySetDefinition, OpenMode.ForRead);

        PropertyDefinitionCollection propDef = setDef.Definitions;
        var propDefs = new Dictionary<int, PropertyDefinition>();
        foreach (PropertyDefinition def in propDef) propDefs.Add(def.Id, def);

        foreach (PropertySetData data in propertySet.PropertySetData)
        {
          if (propDefs.ContainsKey(data.Id))
            setDictionary.Add(propDefs[data.Id].Name, data.GetData());
          else
            setDictionary.Add(data.FieldBucketId, data.GetData());
        }

        if (setDictionary.Count > 0)
          sets.Add(CleanDictionary(setDictionary));
      }
      return sets;
    }

    // Handles object types from property set dictionaries
    private static Dictionary<string, object> CleanDictionary(Dictionary<string, object> dict)
    {
      var target = new Dictionary<string, object>();
      foreach (var key in dict.Keys)
      {
        var obj = dict[key];
        switch (obj)
        {
          case double _:
          case bool _:
          case int _:
          case string _:
          case IEnumerable<double> _:
          case IEnumerable<bool> _:
          case IEnumerable<int> _:
          case IEnumerable<string> _:
            target[key] = obj;
            continue;

          case long o:
            target[key] = Convert.ToDouble(o);
            continue;

          case ObjectId o:
            target[key] = o.ToString();
            continue;

          default:
            continue;
        }
      }
      return target;
    }
#endif

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

    public static void SetStyle(Base styleBase, Entity entity, Dictionary<string, ObjectId> lineTypeDictionary)
    {
      var units = styleBase["units"] as string;
      var color = styleBase["color"] as int?;
      if (color == null) color = styleBase["diffuse"] as int?; // in case this is from a rendermaterial base
      var lineType = styleBase["linetype"] as string;
      var lineWidth = styleBase["lineweight"] as double?;

      if (color != null)
      {
        var systemColor = System.Drawing.Color.FromArgb((int)color);
        entity.Color = Color.FromRgb(systemColor.R, systemColor.G, systemColor.B);
        entity.Transparency = new Transparency(systemColor.A);
      }

      double conversionFactor = (units != null) ? Units.GetConversionFactor(Units.GetUnitsFromString(units), Units.Millimeters) : 1;
      if (lineWidth != null)
        entity.LineWeight = GetLineWeight((double)lineWidth * conversionFactor);

      if (lineType != null)
        if (lineTypeDictionary.ContainsKey(lineType))
          entity.LinetypeId = lineTypeDictionary[lineType];
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
