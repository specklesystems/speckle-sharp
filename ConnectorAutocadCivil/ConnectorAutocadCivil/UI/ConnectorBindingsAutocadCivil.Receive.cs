using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using static Speckle.ConnectorAutocadCivil.Utils;

#if ADVANCESTEEL
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI
{
  public partial class ConnectorBindingsAutocad : ConnectorBindings
  {
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      if (Doc == null)
        throw new InvalidOperationException("No Document is open");

      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);

      var stream = await state.Client.StreamGet(state.StreamId);

      Commit commit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
      state.LastCommit = commit;

      Base commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);
      await ConnectorHelpers.TryCommitReceived(state, commit, Utils.VersionedAppName, progress.CancellationToken);

      // invoke conversions on the main thread via control
      try
      {
        if (Control.InvokeRequired)
          Control.Invoke(
            new ReceivingDelegate(ConvertReceiveCommit),
            commitObject,
            converter,
            state,
            progress,
            stream,
            commit.id
          );
        else
          ConvertReceiveCommit(commitObject, converter, state, progress, stream, commit.id);
      }
      catch (Exception ex)
      {
        throw new Exception($"Could not convert commit: {ex.Message}", ex);
      }

      return state;
    }

    delegate void ReceivingDelegate(
      Base commitObject,
      ISpeckleConverter converter,
      StreamState state,
      ProgressViewModel progress,
      Stream stream,
      string id
    );

    private void ConvertReceiveCommit(
      Base commitObject,
      ISpeckleConverter converter,
      StreamState state,
      ProgressViewModel progress,
      Stream stream,
      string id
    )
    {
      using (DocumentLock l = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          // set the context doc for conversion - this is set inside the transaction loop because the converter retrieves this transaction for all db editing when the context doc is set!
          converter.SetContextDocument(Doc);
          converter.ReceiveMode = state.ReceiveMode;

          // set converter settings as tuples (setting slug, setting selection)
          var settings = new Dictionary<string, string>();
          CurrentSettings = state.Settings;
          foreach (var setting in state.Settings)
            settings.Add(setting.Slug, setting.Selection);
          converter.SetConverterSettings(settings);

          // keep track of conversion progress here
          progress.Report = new ProgressReport();
          var conversionProgressDict = new ConcurrentDictionary<string, int>();
          conversionProgressDict["Conversion"] = 0;

          // create a commit prefix: used for layers and block definition names
          var commitPrefix = Formatting.CommitInfo(stream.name, state.BranchName, id);

          // give converter a way to access the commit info
          if (Doc.UserData.ContainsKey("commit"))
            Doc.UserData["commit"] = commitPrefix;
          else
            Doc.UserData.Add("commit", commitPrefix);

          // delete existing commit layers
          try
          {
            DeleteBlocksWithPrefix(commitPrefix, tr);
          }
          catch
          {
            converter.Report.LogOperationError(
              new Exception(
                $"Failed to remove existing layers or blocks starting with {commitPrefix} before importing new geometry."
              )
            );
          }

          // clear previously stored objects
          StoredObjects.Clear();

          // flatten the commit object to retrieve children objs
          var commitObjs = FlattenCommitObject(commitObject, converter);

          // open model space block table record for write
          BlockTableRecord btr = (BlockTableRecord)tr.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite);

          // Get doc line types for bake: more efficient this way than doing this per object
          LineTypeDictionary.Clear();
          var lineTypeTable = (LinetypeTable)tr.GetObject(Doc.Database.LinetypeTableId, OpenMode.ForRead);
          foreach (ObjectId lineTypeId in lineTypeTable)
          {
            var linetype = (LinetypeTableRecord)tr.GetObject(lineTypeId, OpenMode.ForRead);
            LineTypeDictionary.Add(linetype.Name, lineTypeId);
          }

          #region layer creation

          Application.UIBindings.Collections.Layers.CollectionChanged -= Application_LayerChanged; // temporarily unsubscribe from layer handler since layer creation will trigger it.

          // sort by depth and create all the containers as layers first
          var layers = new Dictionary<string, string>();
          var containers = commitObjs.Select(o => o.Container).Distinct().ToList();

          // create the layers
          var layerTable = (LayerTable)tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead); // get the layer table
          foreach (var container in containers)
          {
            var path = state.ReceiveMode == ReceiveMode.Create ? $"{commitPrefix}${container}" : container;
            string layer = null;

            // try to see if there's a collection object first
            var collection = commitObjs
              .Where(o => o.Container == container && o.Descriptor.Contains("Collection"))
              .FirstOrDefault();
            if (collection != null)
            {
              var storedCollection = StoredObjects[collection.OriginalId];
              storedCollection["path"] = path; // needed by converter
              converter.Report.Log(collection); // Log object so converter can access
              var convertedCollection = converter.ConvertToNative(storedCollection) as List<object>;
              if (convertedCollection != null && convertedCollection.Count > 0)
              {
                layer = (convertedCollection.First() as LayerTableRecord)?.Name;
                commitObjs[commitObjs.IndexOf(collection)] = converter.Report.ReportObjects[collection.OriginalId];
              }
            }
            // otherwise create the layer here (old commits before collections implementations, or from apps not supporting collections)
            else
            {
              if (GetOrMakeLayer(path, tr, out string cleanName))
              {
                layer = cleanName;
              }
            }

            if (layer == null)
            {
              progress.Report.ConversionErrors.Add(
                new Exception($"Could not create layer [{path}]. Objects will be placed on default layer.")
              );
              continue;
            }

            layers.Add(path, layer);
          }
          #endregion


          // conversion
          foreach (var commitObj in commitObjs)
          {
            // handle user cancellation
            if (progress.CancellationToken.IsCancellationRequested)
              return;

            // convert base (or base fallback values) and store in appobj converted prop
            if (commitObj.Convertible)
            {
              converter.Report.Log(commitObj); // Log object so converter can access
              try
              {
                commitObj.Converted = ConvertObject(commitObj, converter);
              }
              catch (Exception e)
              {
                commitObj.Log.Add($"Failed conversion: {e.Message}");
              }
            }
            else
              foreach (var fallback in commitObj.Fallback)
              {
                try
                {
                  fallback.Converted = ConvertObject(fallback, converter);
                }
                catch (Exception e)
                {
                  commitObj.Log.Add($"Fallback {fallback.applicationId} failed conversion: {e.Message}");
                }
                commitObj.Log.AddRange(fallback.Log);
              }

            // if the object wasnt converted, log fallback status
            if (commitObj.Converted == null || commitObj.Converted.Count == 0)
            {
              var convertedFallback = commitObj.Fallback.Where(o => o.Converted != null || o.Converted.Count > 0);
              if (convertedFallback != null && convertedFallback.Count() > 0)
                commitObj.Update(logItem: $"Creating with {convertedFallback.Count()} fallback values");
              else
                commitObj.Update(
                  status: ApplicationObject.State.Failed,
                  logItem: $"Couldn't convert object or any fallback values"
                );
            }

            // add to progress report
            progress.Report.Log(commitObj);
          }
          progress.Report.Merge(converter.Report);

          // add applicationID xdata before bake
          if (!ApplicationIdManager.AddApplicationIdXDataToDoc(Doc, tr))
          {
            progress.Report.LogOperationError(new Exception("Could not create document application id reg table"));
            return;
          }

          // handle operation errors
          if (progress.Report.OperationErrorsCount != 0)
            return;

          // bake
          var fileNameHash = GetDocumentId();
          foreach (var commitObj in commitObjs)
          {
            // handle user cancellation
            if (progress.CancellationToken.IsCancellationRequested)
              return;

            // find existing doc objects if they exist
            var existingObjs = new List<ObjectId>();
            var layer = layers.ContainsKey(commitObj.Container) ? layers[commitObj.Container] : "0";
            if (state.ReceiveMode == ReceiveMode.Update) // existing objs will be removed if it exists in the received commit
            {
              existingObjs = ApplicationIdManager.GetObjectsByApplicationId(
                Doc,
                tr,
                commitObj.applicationId,
                fileNameHash
              );
            }

            // bake
            if (commitObj.Convertible)
            {
              BakeObject(commitObj, converter, tr, layer, existingObjs);
              commitObj.Status = !commitObj.CreatedIds.Any()
                ? ApplicationObject.State.Failed
                : existingObjs.Count > 0
                  ? ApplicationObject.State.Updated
                  : ApplicationObject.State.Created;
            }
            else
            {
              foreach (var fallback in commitObj.Fallback)
                BakeObject(fallback, converter, tr, layer, existingObjs, commitObj);
              commitObj.Status =
                commitObj.Fallback.Where(o => o.Status == ApplicationObject.State.Failed).Count()
                == commitObj.Fallback.Count
                  ? ApplicationObject.State.Failed
                  : existingObjs.Count > 0
                    ? ApplicationObject.State.Updated
                    : ApplicationObject.State.Created;
            }
            Autodesk.AutoCAD.Internal.Utils.FlushGraphics();

            // log to progress report and update progress
            progress.Report.Log(commitObj);
            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);
          }

          // remove commit info from doc userdata
          Doc.UserData.Remove("commit");

          tr.Commit();
        }
      }
    }

    private List<ApplicationObject> FlattenCommitObject(Base obj, ISpeckleConverter converter)
    {
      //TODO: this implementation is almost identical to Rhino, we should try and extract as much of it as we can into Core
      void StoreObject(Base @base, ApplicationObject appObj)
      {
        if (StoredObjects.ContainsKey(@base.id))
          appObj.Update(logItem: "Found another object in this commit with the same id. Skipped other object"); //TODO check if we are actually ignoring duplicates, since we are returning the app object anyway...
        else
          StoredObjects.Add(@base.id, @base);
      }

      ApplicationObject CreateApplicationObject(Base current, string containerId)
      {
        ApplicationObject NewAppObj()
        {
          var speckleType = current.speckle_type
            .Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
          return new ApplicationObject(current.id, speckleType)
          {
            applicationId = current.applicationId,
            Container = containerId
          };
        }

        // skip if it is the base commit collection
        if (current is Collection && string.IsNullOrEmpty(containerId))
          return null;

        //Handle convertable objects
        if (converter.CanConvertToNative(current))
        {
          var appObj = NewAppObj();
          appObj.Convertible = true;
          StoreObject(current, appObj);
          return appObj;
        }

        //Handle objects convertable using displayValues
        var fallbackMember = current["displayValue"] ?? current["@displayValue"];
        if (fallbackMember != null)
        {
          var appObj = NewAppObj();
          var fallbackObjects = GraphTraversal
            .TraverseMember(fallbackMember)
            .Select(o => CreateApplicationObject(o, containerId));
          appObj.Fallback.AddRange(fallbackObjects);

          StoreObject(current, appObj);
          return appObj;
        }

        return null;
      }

      string LayerId(TraversalContext context) => LayerIdRecurse(context, new StringBuilder()).ToString();
      StringBuilder LayerIdRecurse(TraversalContext context, StringBuilder stringBuilder)
      {
        if (context.propName == null)
          return stringBuilder;

        string objectLayerName = string.Empty;
        if (context.propName.ToLower() == "elements" && context.current is Collection coll)
          objectLayerName = coll.name;
        else if (context.propName.ToLower() != "elements") // this is for any other property on the collection. skip elements props in layer structure.
          objectLayerName = context.propName[0] == '@' ? context.propName.Substring(1) : context.propName;
        LayerIdRecurse(context.parent, stringBuilder);
        if (stringBuilder.Length != 0 && !string.IsNullOrEmpty(objectLayerName))
          stringBuilder.Append('$');
        stringBuilder.Append(objectLayerName);

        return stringBuilder;
      }

      var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

      var objectsToConvert = traverseFunction
        .Traverse(obj)
        .Select(tc => CreateApplicationObject(tc.current, LayerId(tc)))
        .Where(appObject => appObject != null)
        .Reverse() //just for the sake of matching the previous behaviour as close as possible
        .ToList();

      return objectsToConvert;
    }

    private List<object> ConvertObject(ApplicationObject appObj, ISpeckleConverter converter)
    {
      var obj = StoredObjects[appObj.OriginalId];
      var convertedList = new List<object>();

      var converted = converter.ConvertToNative(obj);
      if (converted == null)
        return convertedList;

      //Iteratively flatten any lists
      void FlattenConvertedObject(object item)
      {
        if (item is IList list)
          foreach (object child in list)
            FlattenConvertedObject(child);
        else
          convertedList.Add(item);
      }
      FlattenConvertedObject(converted);

      return convertedList;
    }

    private void BakeObject(
      ApplicationObject appObj,
      ISpeckleConverter converter,
      Transaction tr,
      string layer,
      List<ObjectId> toRemove,
      ApplicationObject parent = null
    )
    {
      var obj = StoredObjects[appObj.OriginalId];
      int bakedCount = 0;
      bool remove =
        appObj.Status == ApplicationObject.State.Created
        || appObj.Status == ApplicationObject.State.Updated
        || appObj.Status == ApplicationObject.State.Failed
          ? false
          : true;

      foreach (var convertedItem in appObj.Converted)
      {
        switch (convertedItem)
        {
          case Entity o:

            if (o == null)
              continue;

            var res = o.Append(layer);
            if (res.IsValid)
            {
              // handle display - fallback to rendermaterial if no displaystyle exists
              Base display = obj[@"displayStyle"] as Base;
              if (display == null)
                display = obj[@"renderMaterial"] as Base;
              if (display != null)
                Utils.SetStyle(display, o, LineTypeDictionary);

              // add property sets if this is Civil3D
#if CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024
                try
                {
                  if (obj["propertySets"] is IReadOnlyList<object> list)
                  {
                    var propertySets = new List<Dictionary<string, object>>();
                    foreach (var listObj in list)
                      propertySets.Add(listObj as Dictionary<string, object>);
                    o.SetPropertySets(Doc, propertySets);
                  }
                }
                catch (Exception e)
                {
                  appObj.Log.Add($"Could not attach property sets: {e.Message}");
                }
#endif

              // set application id
              var appId = parent != null ? parent.applicationId : obj.applicationId;
              var newObj = tr.GetObject(res, OpenMode.ForWrite);
              if (!ApplicationIdManager.SetObjectCustomApplicationId(newObj, appId, out appId))
              {
                appObj.Log.Add($"Could not attach applicationId xdata");
              }

              tr.TransactionManager.QueueForGraphicsFlush();

              if (parent != null)
                parent.Update(createdId: res.Handle.ToString());
              else
                appObj.Update(createdId: res.Handle.ToString());

              bakedCount++;
            }
            else
            {
              var bakeMessage = $"Could not bake to document.";
              if (parent != null)
                parent.Update(logItem: $"fallback {appObj.applicationId}: {bakeMessage}");
              else
                appObj.Update(logItem: bakeMessage);
              continue;
            }
            break;
          default:
            break;
        }
      }

      if (bakedCount == 0)
      {
        if (parent != null)
          parent.Update(logItem: $"fallback {appObj.applicationId}: could not bake object");
        else
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not bake object");
      }
      else
      {
        // remove existing objects if they exist
        if (remove)
        {
          foreach (var objId in toRemove)
          {
            try
            {
              DBObject objToRemove = tr.GetObject(objId, OpenMode.ForWrite);
              objToRemove.Erase();
            }
            catch (Exception e)
            {
              if (!e.Message.Contains("eWasErased")) // this couldve been previously received and deleted
              {
                if (parent != null)
                  parent.Log.Add(e.Message);
                else
                  appObj.Log.Add(e.Message);
              }
            }
          }
          appObj.Status = toRemove.Count > 0 ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
        }
      }
    }

    private void DeleteBlocksWithPrefix(string prefix, Transaction tr)
    {
      BlockTable blockTable = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
      foreach (ObjectId blockId in blockTable)
      {
        BlockTableRecord block = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
        if (block.Name.StartsWith(prefix))
        {
          block.UpgradeOpen();
          block.Erase();
        }
      }
    }

    private bool GetOrMakeLayer(string layerName, Transaction tr, out string cleanName)
    {
      cleanName = Utils.RemoveInvalidChars(layerName);
      try
      {
        LayerTable layerTable = tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
        if (layerTable.Has(cleanName))
        {
          return true;
        }
        else
        {
          layerTable.UpgradeOpen();
          var _layer = new LayerTableRecord();

          // Assign the layer properties
          _layer.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 7); // white
          _layer.Name = cleanName;

          // Append the new layer to the layer table and the transaction
          layerTable.Add(_layer);
          tr.AddNewlyCreatedDBObject(_layer, true);
        }
      }
      catch
      {
        return false;
      }
      return true;
    }
  }
}
