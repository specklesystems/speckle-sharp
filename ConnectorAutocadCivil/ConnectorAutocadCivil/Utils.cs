using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.ConnectorAutocadCivil.DocumentUtils;
using Speckle.Core.Logging;

#if CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024
using Autodesk.Aec.ApplicationServices;
using Autodesk.Aec.PropertyData.DatabaseServices;
#endif

#if ADVANCESTEEL
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
using Autodesk.AdvanceSteel.Connection;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Modelling;
#endif

namespace Speckle.ConnectorAutocadCivil;

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
#elif AUTOCAD2024
  public static string VersionedAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2024);
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
#elif CIVIL2024
  public static string VersionedAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2024);
  public static string AppName = HostApplications.Civil.Name;
  public static string Slug = HostApplications.Civil.Slug;
#elif ADVANCESTEEL2023
  public static string VersionedAppName = HostApplications.AdvanceSteel.GetVersion(HostAppVersion.v2023);
  public static string AppName = HostApplications.AdvanceSteel.Name;
  public static string Slug = HostApplications.AdvanceSteel.Slug;
#elif ADVANCESTEEL2024
  public static string VersionedAppName = HostApplications.AdvanceSteel.GetVersion(HostAppVersion.v2024);
  public static string AppName = HostApplications.AdvanceSteel.Name;
  public static string Slug = HostApplications.AdvanceSteel.Slug;
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
    {
      return handles;
    }

    Document Doc = Application.DocumentManager.MdiActiveDocument;
    using (TransactionContext.StartTransaction(Doc))
    {
      Transaction tr = Doc.TransactionManager.TopTransaction;
      foreach (SelectedObject selObj in selection)
      {
        DBObject obj = tr.GetObject(selObj.ObjectId, OpenMode.ForRead);
        if (obj != null && obj.Visible())
        {
#if ADVANCESTEEL

          if (CheckAdvanceSteelObject(obj))
          {
            ASFilerObject filerObject = GetFilerObjectByEntity<ASFilerObject>(obj);
            if (filerObject is FeatureObject || filerObject is PlateFoldRelation) //Don't select features objects, they are going with Advance Steel objects
            {
              continue;
            }
          }
#endif

          handles.Add(obj.Handle.ToString());
        }
      }
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
    var db = entity.Database ?? Application.DocumentManager.MdiActiveDocument.Database;
    Transaction tr = db.TransactionManager.TopTransaction;
    if (tr == null)
    {
      return ObjectId.Null;
    }

    BlockTableRecord btr = db.GetModelSpace(OpenMode.ForWrite);
    if (entity.IsNewObject)
    {
      if (layer != null)
      {
        entity.Layer = layer;
      }

      var id = btr.AppendEntity(entity);
      tr.AddNewlyCreatedDBObject(entity, true);
      return id;
    }
    else
    {
      if (layer != null)
      {
        entity.Layer = layer;
      }

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
  /// Get visibility of a DBObject
  /// </summary>
  /// <param name="obj"></param>
  /// <returns>True if the object is visible, or false if not. Returns true on failure to retrieve object visibility property.</returns>
  public static bool Visible(this DBObject obj)
  {
    bool isVisible = true;

    if (obj is Entity)
    {
      Entity ent = obj as Entity;

      if (!ent.Visible)
      {
        return ent.Visible;
      }

      Document Doc = Application.DocumentManager.MdiActiveDocument;
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        LayerTableRecord lyrTblRec = tr.GetObject(ent.LayerId, OpenMode.ForRead) as LayerTableRecord;
        if (lyrTblRec.IsOff)
        {
          isVisible = false;
        }

        tr.Commit();
      }
    }
    else
    {
      try
      {
        PropertyInfo prop = obj.GetType().GetProperty("Visible");
        if (prop.GetValue(obj) is bool visible)
        {
          isVisible = visible;
        }
      }
      catch (AmbiguousMatchException) { } // will return true on failure to retrieve object visibility property.
    }
    return isVisible;
  }

  public static bool GetOrMakeLayer(this Document doc, string layerName, Transaction tr, out string cleanName)
  {
    cleanName = RemoveInvalidChars(layerName);
    if (tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) is LayerTable layerTable)
    {
      if (layerTable.Has(cleanName))
      {
        return true;
      }
      else
      {
        layerTable.UpgradeOpen();
        LayerTableRecord newLayer =
          new()
          {
            Color = Color.FromColorIndex(ColorMethod.ByColor, 7), // white
            Name = cleanName
          };

        // Append the new layer to the layer table and the transaction
        try
        {
          layerTable.Add(newLayer);
        }
        catch (Exception e) when (!e.IsFatal())
        {
          // Objects on this layer will be placed on the default layer instead
          SpeckleLog.Logger.Error(e, $"Could not add new layer {cleanName} to the layer table");
          return false;
        }
        tr.AddNewlyCreatedDBObject(newLayer, true);
      }
    }
    return true;
  }

  #endregion

  #region property sets
#if CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024
  private static Autodesk.Aec.PropertyData.DataType? GetPropertySetType(object prop)
  {
    switch (prop)
    {
      case IEnumerable<string>:
      case IEnumerable<int>:
      case IEnumerable<double>:
      case IEnumerable<bool>:
        return Autodesk.Aec.PropertyData.DataType.List;

      case string:
        return Autodesk.Aec.PropertyData.DataType.Text;
      case int:
        return Autodesk.Aec.PropertyData.DataType.Integer;
      case double:
        return Autodesk.Aec.PropertyData.DataType.Real;
      case bool:
        return Autodesk.Aec.PropertyData.DataType.TrueFalse;

      default:
        return null;
    }
  }

  public static PropertySetDefinition CreatePropertySet(Dictionary<string,object> propertySetDict, Document doc)
  {
    PropertySetDefinition propSetDef = new();
    propSetDef.SetToStandard(doc.Database);
    propSetDef.SubSetDatabaseDefaults(doc.Database);
    propSetDef.Description = "Property Set Definition added with Speckle";
    propSetDef.AppliesToAll = true;

    // Create the definition for each property
    foreach (var entry in propertySetDict)
    {
      if (GetPropertySetType(entry.Value) is Autodesk.Aec.PropertyData.DataType dataType)
      {
        var propDef = new PropertyDefinition
        {
          DataType = dataType,
          Name = entry.Key,
          DefaultData = entry.Value
        };

        propDef.SetToStandard(doc.Database);
        propDef.SubSetDatabaseDefaults(doc.Database);
        propSetDef.Definitions.Add(propDef);
      }
      else
      {
        SpeckleLog.Logger.Error( $"Could not determine property set entry type of {entry.Value}. Property set entry not added to property set definitions.");
      }
    }

    return propSetDef;
  }

  /// <summary>
  /// Finds a property set by its ObjectId on a given object.
  /// </summary>
  /// <param name="obj">The object to find the property set on.</param>
  /// <param name="propertySetId">The property set ObjectId to find on the object.</param>
  /// <returns> True if the property set with the given ObjectId was found, or false otherwise. </returns>
  public static bool ObjectHasPropertySet(DBObject obj, ObjectId propertySetId)
  {
    ObjectId temporaryId = ObjectId.Null;

    try
    {
      temporaryId = PropertyDataServices.GetPropertySet(obj, propertySetId);
    } 
    catch (Autodesk.AutoCAD.Runtime.Exception e) when (!e.IsFatal())
    {
      // This will throw if the property set does not exist on the object.
      // afaik, trycatch is necessary because there is no way to preemptively check if the set already exists.
      // More than likely a runtime exception with message: eKeyNotFound.
    }

    return temporaryId != ObjectId.Null;
  }

  /// <summary>
  /// Creates a property set on a given object.
  /// Requires that the property set exists in the current database and
  /// that the property set applies to the object.
  /// </summary>
  /// <param name="obj">The  object to create the property set on.</param>
  /// <param name="propertySetId">The objectID of the property set to create on the object </param>
  /// <returns> True if the property set was created on the object, or false if there was a failure. </returns>
  public static void AddPropertySetToObject(DBObject obj, ObjectId propertySetId)
  {
    try
    {
      if (!ObjectHasPropertySet(obj, propertySetId))
      {
        if (!obj.IsWriteEnabled)
        {
          obj.UpgradeOpen();
        }

        PropertyDataServices.AddPropertySet(obj, propertySetId);
      }
    }
    catch (Autodesk.AutoCAD.Runtime.Exception e)
    {
      throw new InvalidOperationException($"Could not create property set on object {obj.Id}", e);
    }
  } 

  public static void SetPropertySets(this Entity entity, Document doc, List<Dictionary<string, object>> propertySetDicts)
  {
    // create a dictionary for property sets for this object
    var name = $"Speckle {entity.Handle} Property Set";
    int count = 0;
    using DictionaryPropertySetDefinitions dictPropSetDef = new(doc.Database);

    // add property sets to object
    using Transaction tr = doc.Database.TransactionManager.StartTransaction();
    foreach (Dictionary<string, object> propertySetDict in propertySetDicts)
    {
      try
      {
        // create the property set definition for this set
        PropertySetDefinition propSetDef = CreatePropertySet(propertySetDict, doc);
        var propSetDefName = name += $" - {count}";
        // add property set to the database
        dictPropSetDef.AddNewRecord(propSetDefName, propSetDef);
        tr.AddNewlyCreatedDBObject(propSetDef, true);
        // add property set to the object
        AddPropertySetToObject(entity, propSetDef.ObjectId);
      }
      catch (Autodesk.AutoCAD.Runtime.Exception) { }

      count++;
    }

    tr.Commit();
  }

  /// <summary>
  /// Get the property sets of DBObject
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
    catch (Autodesk.AutoCAD.Runtime.Exception e) 
    {
      // This may throw if property sets do not exist on the object.
      // afaik, trycatch is necessary because there is no way to preemptively check if the set already exists.
    }

    if (propertySets is null)
    {
      return sets;
    }

    foreach (ObjectId id in propertySets)
    {
      var setDictionary = new Dictionary<string, object>();

      PropertySet propertySet = (PropertySet)tr.GetObject(id, OpenMode.ForRead);
      PropertySetDefinition setDef = (PropertySetDefinition)tr.GetObject(propertySet.PropertySetDefinition, OpenMode.ForRead);

      PropertyDefinitionCollection propDef = setDef.Definitions;
      var propDefs = new Dictionary<int, PropertyDefinition>();
      foreach (PropertyDefinition def in propDef)
      {
        propDefs.Add(def.Id, def);
      }

      foreach (PropertySetData data in propertySet.PropertySetData)
      {
        if (propDefs.TryGetValue(data.Id, out PropertyDefinition value))
        {
          setDictionary.Add(value.Name, data.GetData());
        }
        else
        {
          setDictionary.Add(data.FieldBucketId, data.GetData());
        }
      }

      if (setDictionary.Count > 0)
      {
        sets.Add(CleanDictionary(setDictionary));
      }
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
        case double:
        case bool:
        case int:
        case string:
        case IEnumerable<double>:
        case IEnumerable<bool>:
        case IEnumerable<int>:
        case IEnumerable<string>:
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
  #endregion

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
      BlockTableRecord blckTblRcrd =
        tr.GetObject(blckTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
      foreach (ObjectId id in blckTblRcrd)
      {
        DBObject dbObj = tr.GetObject(id, OpenMode.ForRead);
        if (converter.CanConvertToSpeckle(dbObj) && dbObj.Visible())
        {
          objs.Add(dbObj.Handle.ToString());
        }
      }
      tr.Commit();
    }
    return objs;
  }

  #region application id
  public static class ApplicationIdManager
  {
    static readonly string ApplicationIdKey = "applicationId";

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
          using RegAppTableRecord regAppRecord = new();
          regAppRecord.Name = ApplicationIdKey;
          regAppTable.UpgradeOpen();
          regAppTable.Add(regAppRecord);
          regAppTable.DowngradeOpen();
          tr.AddNewlyCreatedDBObject(regAppRecord, true);
        }
        catch (Exception e) when (!e.IsFatal())
        {
          SpeckleLog.Logger.Error(e, "Could not create the RegAppTableRecord for application ids in the Doc.");
          return false;
        }
      }
      return true;
    }

    public static string GetFromXData(Entity obj)
    {
      string appId = null;
      if (!obj.IsReadEnabled)
      {
        obj.UpgradeOpen();
      }

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
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool SetObjectCustomApplicationId(
      DBObject obj,
      string id,
      out string applicationId,
      string fileNameHash = null
    )
    {
      applicationId = fileNameHash == null ? id : $"{fileNameHash}-{id}";
      var rb = new ResultBuffer(
        new TypedValue((int)DxfCode.ExtendedDataRegAppName, ApplicationIdKey),
        new TypedValue(1000, applicationId)
      );

      try
      {
        if (!obj.IsWriteEnabled)
        {
          obj.UpgradeOpen();
        }

        obj.XData = rb;
      }
      catch (Autodesk.AutoCAD.Runtime.Exception)
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
    public static List<ObjectId> GetObjectsByApplicationId(
      Document doc,
      Transaction tr,
      string appId,
      string fileNameHash
    )
    {
      var foundObjects = new List<ObjectId>();
      if (string.IsNullOrEmpty(appId))
      {
        return foundObjects;
      }
      // first check for custom xdata application ids, because object handles tend to be duplicated

      // Create a TypedValue array to define the filter criteria
      TypedValue[] acTypValAr = new TypedValue[1];
      acTypValAr.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ApplicationIdKey), 0);

      // Create a selection filter for the applicationID xdata entry and find all objs with this field
      SelectionFilter acSelFtr = new(acTypValAr);
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

      if (foundObjects.Count != 0)
      {
        return foundObjects;
      }

      // if no matching xdata appids were found, loop through handles instead
      var autocadAppIdParts = appId.Split('-');
      if (autocadAppIdParts.Length == 2 && autocadAppIdParts.FirstOrDefault().StartsWith(fileNameHash))
      {
        if (GetHandle(autocadAppIdParts.Last(), out Handle handle))
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
    if (obj == null)
    {
      return string.Empty;
    }

    var simpleType = obj.GetType().Name;
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

#if CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024
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
    if (string.IsNullOrEmpty(str))
    {
      return false;
    }

    long l;
    try
    {
      l = Convert.ToInt64(str, 16);
    }
    catch (ArgumentException)
    {
      return false;
    }
    catch (FormatException)
    {
      return false;
    }
    catch (OverflowException)
    {
      return false;
    }

    handle = new Handle(l);

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
    var color = styleBase["color"] as int?;
    color ??= styleBase["diffuse"] as int?; // in case this is from a rendermaterial base

    var transparency = styleBase["opacity"] as double?;
    var lineWidth = styleBase["lineweight"] as double?;

    if (color != null)
    {
      var systemColor = System.Drawing.Color.FromArgb((int)color);
      entity.Color = Color.FromRgb(systemColor.R, systemColor.G, systemColor.B);
      var alpha =
        transparency != null
          ? (byte)(transparency * 255d) //render material
          : systemColor.A; //display style
      entity.Transparency = new Transparency(alpha);
    }

    double conversionFactor =
      (styleBase["units"] is string units)
        ? Units.GetConversionFactor(Units.GetUnitsFromString(units), Units.Millimeters)
        : 1;
    if (lineWidth != null)
    {
      entity.LineWeight = GetLineWeight((double)lineWidth * conversionFactor);
    }

    if (styleBase["linetype"] is string lineType)
    {
      if (lineTypeDictionary.TryGetValue(lineType, out ObjectId value))
      {
        entity.LinetypeId = value;
      }
    }
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

#if ADVANCESTEEL

  public static T GetFilerObjectByEntity<T>(DBObject @object) where T : ASFilerObject
  {
    ASObjectId idCadEntity = new(@object.ObjectId.OldIdPtr);
    ASObjectId idFilerObject = Autodesk.AdvanceSteel.CADAccess.DatabaseManager.GetFilerObjectId(idCadEntity, false);
    if (idFilerObject.IsNull())
    {
      return null;
    }

    return Autodesk.AdvanceSteel.CADAccess.DatabaseManager.Open(idFilerObject) as T;
  }

  public static bool CheckAdvanceSteelObject(DBObject @object)
  {
    return @object.ObjectId.ObjectClass.DxfName.IndexOf("AST") == 0;
  }

#endif
}
