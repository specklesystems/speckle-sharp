using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace AutocadCivilDUI3Shared.Utils
{
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
        catch (Exception e)
        {
          return false;
        }
      }
      return true;
    }

    public static string GetFromXData(Entity obj)
    {
      string appId = null;
      if (!obj.IsReadEnabled)
        obj.UpgradeOpen();

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
          obj.UpgradeOpen();
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
    public static List<ObjectId> GetObjectsByApplicationId(
      Document doc,
      Transaction tr,
      string appId,
      string fileNameHash
    )
    {
      var foundObjects = new List<ObjectId>();
      if (string.IsNullOrEmpty(appId))
        return foundObjects;
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
      if (foundObjects.Any())
        return foundObjects;

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
}
