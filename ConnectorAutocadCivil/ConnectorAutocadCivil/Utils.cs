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

#if CIVIL2021 || CIVIL2022 || CIVIL2023
using Autodesk.Aec.ApplicationServices;
using Autodesk.Aec.PropertyData.DatabaseServices;
#endif

namespace Speckle.ConnectorAutocadCivil
{
  public static class Utils
  {

#if AUTOCAD2021
    public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2021);
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2022
    public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2022);
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif AUTOCAD2023
    public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2023);
    public static string AppName = HostApplications.AutoCAD.Name;
    public static string Slug = HostApplications.AutoCAD.Slug;
#elif CIVIL2021
    public static string VersionedAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2021);
    public static string AppName = HostApplications.Civil.Name;
    public static string Slug = HostApplications.Civil.Slug;
#elif CIVIL2022
    public static string VersionedAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2022);
    public static string AppName = HostApplications.Civil.Name;
    public static string Slug = HostApplications.Civil.Slug;
#elif CIVIL2023
    public static string VersionedAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2023);
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
    /// Used to retrieve a DB Object from its handle
    /// </summary>
    /// <param name="handle">Object handle as string</param>
    /// <param name="type">Object class dxf name</param>
    /// <param name="layer">Object layer name</param>
    /// <returns></returns>
    public static DBObject GetObject(this Handle handle, Transaction tr, out string type, out string layer, out string applicationId)
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

#if CIVIL2021 || CIVIL2022 || CIVIL2023
    private static Autodesk.Aec.PropertyData.DataType? GetPropertySetType(object prop)
    {
      switch (prop)
      {
        case IEnumerable<string> _:
        case IEnumerable<int> _:
        case IEnumerable<double> _:
        case IEnumerable<bool> _:
          return Autodesk.Aec.PropertyData.DataType.List;

        case string _:
          return Autodesk.Aec.PropertyData.DataType.Text;
        case int _:
          return Autodesk.Aec.PropertyData.DataType.Integer;
        case double _:
          return Autodesk.Aec.PropertyData.DataType.Real;
        case bool _:
          return Autodesk.Aec.PropertyData.DataType.TrueFalse;

        default:
          return null;
      }
    }

    public static void SetPropertySets(this Entity entity, Document doc, List<Dictionary<string, object>> propertySetDicts)
    {
      // create a dictionary for property sets for this object
      var name = $"Speckle {entity.Handle} Property Set";
      int count = 0;
      foreach (var propertySetDict in propertySetDicts)
      {
        // create the property set definition for this set.
        var propSetDef = new PropertySetDefinition();
        propSetDef.SetToStandard(doc.Database);
        propSetDef.SubSetDatabaseDefaults(doc.Database);
        var propSetDefName = name += $" - {count}";
        propSetDef.Description = "Property Set Definition added with Speckle";
        propSetDef.AppliesToAll = true;

        // Create the definition for each property
        foreach (var entry in propertySetDict)
        {
          var propDef = new PropertyDefinition();
          propDef.SetToStandard(doc.Database);
          propDef.SubSetDatabaseDefaults(doc.Database);
          propDef.Name = entry.Key;
          var dataType = GetPropertySetType(entry.Value);
          if (dataType != null)
            propDef.DataType = (Autodesk.Aec.PropertyData.DataType)dataType;
          propDef.DefaultData = entry.Value;
          propSetDef.Definitions.Add(propDef);
        }

        // add the property sets to the object
        try
        {
          // add property set to the database
          // todo: add logging if the property set couldnt be added because a def already exists
          using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
          {
            var dictPropSetDef = new DictionaryPropertySetDefinitions(doc.Database);
            dictPropSetDef.AddNewRecord(propSetDefName, propSetDef);
            tr.AddNewlyCreatedDBObject(propSetDef, true);

            entity.UpgradeOpen();
            PropertyDataServices.AddPropertySet(entity, propSetDef.ObjectId);
            tr.Commit();
          }
        }
        catch { }
      }
    }

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

    #region application id
    public static class ApplicationIdManager
    {
      readonly static string ApplicationIdKey = "applicationId";

      /// <summary>
      /// Creates the application id xdata table in the doc if it doesn't already exist
      /// </summary>
      /// <returns></returns>
      public static bool AddApplicationIdXDataToDoc(Document doc, Transaction tr)
      {
        var regAppTable = (RegAppTable)tr.GetObject(doc.Database.RegAppTableId, OpenMode.ForRead);
        if (!regAppTable.Has(ApplicationIdKey))
        {
          try
          {
            using (RegAppTableRecord regAppRecord = new RegAppTableRecord())
            {
              regAppRecord.Name = ApplicationIdKey;
              regAppTable.UpgradeOpen();
              regAppTable.Add(regAppRecord);
              regAppTable.DowngradeOpen();
              tr.AddNewlyCreatedDBObject(regAppRecord, true);
            }
          }
          catch(Exception e)
          {
            return false;
          }
        }
        return true;
      }

      public static string GetFromXData(Entity obj)
      {
        string appId = null;
        if (!obj.IsReadEnabled) obj.UpgradeOpen();

        ResultBuffer rb = obj.GetXDataForApplication(ApplicationIdKey);
        if (rb != null)
        {
          foreach (var entry in rb)
          {
            if (entry.TypeCode == 1000)
            {
              appId = entry.Value as string;
              break;
            }
          }
        }
        return appId;
      }

      /// <summary>
      /// Attaches a custom application Id to an object's application id xdata using the has of the file name.
      /// This is used because the persistent id of the db object in the file is almost guaranteed to not be unique between files
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="handle"></param>
      /// <returns></returns>
      public static bool SetObjectCustomApplicationId(DBObject obj, string id, out string applicationId, string fileNameHash = null)
      {
        applicationId = fileNameHash == null ? id : $"{fileNameHash}-{id}";
        var rb = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ApplicationIdKey), new TypedValue(1000, applicationId));

        try
        {
          if (!obj.IsWriteEnabled) obj.UpgradeOpen();
          obj.XData = rb;
        }
        catch (Exception e)
        {
          return false;
        }

        return true;
      }

      /// <summary>
      /// Returns, if found, the corresponding doc element.
      /// The doc object can be null if the user deleted it. 
      /// </summary>
      /// <param name="appId">Id of the application that originally created the element, in AutocadCivil it should be "{fileNameHash}-{handle}"</param>
      /// <returns>The element, if found, otherwise null</returns>
      /// <remarks>
      /// Updating can be buggy because of limitations to how object handles are generated. 
      /// See: https://forums.autodesk.com/t5/net/is-the-quot-objectid-quot-unique-in-a-drawing-file/m-p/6527799#M49953
      /// This is temporarily improved by attaching a custom application id xdata "{fileNameHash}-{handle}" to each object when sending, or checking against the fileNameHash on receive
      /// </remarks>
      public static List<ObjectId> GetObjectsByApplicationId(Document doc, Transaction tr, string appId, string fileNameHash)
      {
        var foundObjects = new List<ObjectId>();

        // first check for custom xdata application ids, because object handles tend to be duplicated

        // Create a TypedValue array to define the filter criteria
        TypedValue[] acTypValAr = new TypedValue[1];
        acTypValAr.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ApplicationIdKey), 0);

        // Create a selection filter for the applicationID xdata entry and find all objs with this field
        SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
        var editor = Application.DocumentManager.MdiActiveDocument.Editor;
        var res = editor.SelectAll(acSelFtr);

        if (res.Status != PromptStatus.None && res.Status != PromptStatus.Error)
        {
          // loop through all obj with an appId 
          foreach (var appIdObj in res.Value.GetObjectIds())
          {
            // get the db object from id
            var obj = tr.GetObject(appIdObj, OpenMode.ForRead);
            if (obj != null)
            {
              foreach (var entry in obj.XData)
              {
                if (entry.Value as string == appId)
                {
                  foundObjects.Add(appIdObj);
                  break;
                }
              }
            }
          }
        }
        if (foundObjects.Any()) return foundObjects;

        // if no matching xdata appids were found, loop through handles instead
        var autocadAppIdParts = appId.Split('-');
        if (autocadAppIdParts.Count() == 2 && autocadAppIdParts.FirstOrDefault().StartsWith(fileNameHash))
        {
          if (Utils.GetHandle(autocadAppIdParts.Last(), out Handle handle))
          {
            if (doc.Database.TryGetObjectId(handle, out ObjectId id))
            {
              return id.IsErased ? foundObjects : new List<ObjectId>() { id };
            }
          }
        }

        return foundObjects;
      }
    }
    #endregion
    

    /// <summary>
    /// Returns a descriptive string for reporting
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ObjectDescriptor(DBObject obj)
    {
      if (obj == null) return String.Empty;
      var simpleType = obj.GetType().ToString();
      return $"{simpleType}";
    }

    /// <summary>
    /// Retrieves the document's units.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static string GetUnits(Document doc)
    {
      var insUnits = doc.Database.Insunits;
      string units = (insUnits == UnitsValue.Undefined) ? Units.None : Units.GetUnitsFromString(insUnits.ToString());

#if CIVIL2021 || CIVIL2022 || CIVIL2023
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
    public static bool GetHandle(string str, out Handle handle)
    {
      handle = new Handle();
      try
      {
        handle = new Handle(Convert.ToInt64(str, 16));
      }
      catch { return false; }
      return true;
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
