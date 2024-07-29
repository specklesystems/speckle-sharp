using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using static Speckle.ConnectorAutocadCivil.Utils;

#if ADVANCESTEEL
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI;

public partial class ConnectorBindingsAutocad : ConnectorBindings
{
  public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
  {
    if (Doc == null)
    {
      throw new InvalidOperationException("No Document is open");
    }

    var converter = KitManager.GetDefaultKit().LoadConverter(VersionedAppName);

    var stream = await state.Client.StreamGet(state.StreamId);

    Commit commit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
    state.LastCommit = commit;

    Base commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);
    await ConnectorHelpers.TryCommitReceived(state, commit, VersionedAppName, progress.CancellationToken);

    // invoke conversions on the main thread via control
    try
    {
      if (Control.InvokeRequired)
      {
        Control.Invoke(
          new ReceivingDelegate(ConvertReceiveCommit),
          commitObject,
          converter,
          state,
          progress,
          stream,
          commit.id
        );
      }
      else
      {
        ConvertReceiveCommit(commitObject, converter, state, progress, stream, commit.id);
      }
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
        {
          settings.Add(setting.Slug, setting.Selection);
        }

        converter.SetConverterSettings(settings);

        // keep track of conversion progress here
        progress.Report = new ProgressReport();
        var conversionProgressDict = new ConcurrentDictionary<string, int>();
        conversionProgressDict["Conversion"] = 0;

        // track object types for mixpanel logging
        Dictionary<string, int> typeCountDict = new();

        // create a commit prefix: used for layers and block definition names
        var commitPrefix = Formatting.CommitInfo(stream.name, state.BranchName, id);

        // give converter a way to access the commit info
        if (Doc.UserData.ContainsKey("commit"))
        {
          Doc.UserData["commit"] = commitPrefix;
        }
        else
        {
          Doc.UserData.Add("commit", commitPrefix);
        }

        // delete existing commit layers
        bool success = DeleteBlocksWithPrefix(commitPrefix, tr, out List<string> failedBlocks);
        if (!success)
        {
          converter.Report.LogOperationError(
            new Exception(
              $"Failed to remove {failedBlocks.Count} existing layers or blocks starting with {commitPrefix} before importing new geometry."
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
          var collection = commitObjs.FirstOrDefault(
            o => o.Container == container && o.Descriptor.Contains("Collection")
          );
          if (collection != null)
          {
            var storedCollection = StoredObjects[collection.OriginalId];
            storedCollection["path"] = path; // needed by converter
            converter.Report.Log(collection); // Log object so converter can access
            if (
              converter.ConvertToNative(storedCollection) is List<object> convertedCollection
              && convertedCollection.Count > 0
            )
            {
              layer = (convertedCollection.First() as LayerTableRecord)?.Name;
              commitObjs[commitObjs.IndexOf(collection)] = converter.Report.ReportObjects[collection.OriginalId];
            }
          }
          // otherwise create the layer here (old commits before collections implementations, or from apps not supporting collections)
          else
          {
            if (Doc.GetOrMakeLayer(path, tr, out string cleanName))
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
        foreach (ApplicationObject commitObj in commitObjs)
        {
          // handle user cancellation
          if (progress.CancellationToken.IsCancellationRequested)
          {
            return;
          }

          if (StoredObjects.TryGetValue(commitObj.OriginalId, out Base commitBaseObj))
          {
            // log received object type
            typeCountDict.TryGetValue(commitBaseObj.speckle_type, out var currentCount);
            typeCountDict[commitBaseObj.speckle_type] = ++currentCount;
          }
          else
          {
            commitObj.Update(status: ApplicationObject.State.Failed, logItem: "Object not found in StoredObjects");
          }

          // convert base (or base fallback values) and store in appobj converted prop
          if (commitObj.Convertible)
          {
            converter.Report.Log(commitObj); // Log object so converter can access
            commitObj.Converted = ConvertObject(commitBaseObj, converter);
          }
          else
          {
            foreach (ApplicationObject fallback in commitObj.Fallback)
            {
              if (StoredObjects.TryGetValue(fallback.OriginalId, out Base obj))
              {
                converter.Report.Log(fallback); // Log object so converter can access
                fallback.Converted = ConvertObject(obj, converter);
                commitObj.Log.AddRange(fallback.Log);
              }
              else
              {
                commitObj.Log.Add(
                  $"Fallback object {fallback.OriginalId} not found in StoredObjects and was not converted."
                );
              }
            }
          }

          // if the object wasnt converted, log fallback status
          if (commitObj.Converted.Count == 0)
          {
            var convertedFallbackCount = commitObj.Fallback.Where(o => o.Converted.Count != 0).Count();
            if (convertedFallbackCount > 0)
            {
              commitObj.Update(logItem: $"Creating with {convertedFallbackCount} fallback values");
            }
            else
            {
              commitObj.Update(
                status: ApplicationObject.State.Failed,
                logItem: $"Couldn't convert object or any fallback values"
              );
            }
          }

          // add to progress report
          progress.Report.Log(commitObj);
        }
        progress.Report.Merge(converter.Report);

        // track the object type counts as an event before we try to receive
        // this will tell us the composition of a commit the user is trying to receive, even if it's not successfully received
        // we are capped at 255 properties for mixpanel events, so we need to check dict entries
        var typeCountList = typeCountDict
          .Select(o => new { TypeName = o.Key, Count = o.Value })
          .OrderBy(pair => pair.Count)
          .Reverse()
          .Take(200);

        Analytics.TrackEvent(
          Analytics.Events.ConvertToNative,
          new Dictionary<string, object>() { { "typeCount", typeCountList } }
        );

        // add applicationID xdata before bake
        if (!ApplicationIdManager.AddApplicationIdXDataToDoc(Doc, tr))
        {
          progress.Report.LogOperationError(new Exception("Could not create document application id reg table"));
          return;
        }

        // handle operation errors
        if (progress.Report.OperationErrorsCount != 0)
        {
          return;
        }

        // bake
        var fileNameHash = GetDocumentId();
        foreach (var commitObj in commitObjs)
        {
          // handle user cancellation
          if (progress.CancellationToken.IsCancellationRequested)
          {
            return;
          }

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
            {
              BakeObject(fallback, converter, tr, layer, existingObjs, commitObj);
            }

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
      {
        appObj.Update(logItem: "Found another object in this commit with the same id. Skipped other object"); //TODO check if we are actually ignoring duplicates, since we are returning the app object anyway...
      }
      else
      {
        StoredObjects.Add(@base.id, @base);
      }
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
      {
        return null;
      }

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
      {
        return stringBuilder;
      }

      string objectLayerName = string.Empty;
      if (context.propName.ToLower() == "elements" && context.current is Collection coll)
      {
        objectLayerName = coll.name;
      }
      else if (context.propName.ToLower() != "elements") // this is for any other property on the collection. skip elements props in layer structure.
      {
        objectLayerName = context.propName[0] == '@' ? context.propName.Substring(1) : context.propName;
      }

      LayerIdRecurse(context.parent, stringBuilder);
      if (stringBuilder.Length != 0 && !string.IsNullOrEmpty(objectLayerName))
      {
        stringBuilder.Append('$');
      }

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

  /// <summary>
  /// Converts a Base into its Speckle equivalents
  /// </summary>
  /// <param name="obj"></param>
  /// <param name="converter"></param>
  /// <returns>A list of converted objects, or an empty list if no objects were converted.</returns>
  private List<object> ConvertObject(Base obj, ISpeckleConverter converter)
  {
    var convertedList = new List<object>();

    var converted = converter.ConvertToNative(obj);
    if (converted == null)
    {
      return convertedList;
    }

    //Iteratively flatten any lists
    void FlattenConvertedObject(object item)
    {
      if (item is IList list)
      {
        foreach (object child in list)
        {
          FlattenConvertedObject(child);
        }
      }
      else
      {
        convertedList.Add(item);
      }
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
      appObj.Status
        is not ApplicationObject.State.Created
          and not ApplicationObject.State.Updated
          and not ApplicationObject.State.Failed;

    foreach (var convertedItem in appObj.Converted)
    {
      switch (convertedItem)
      {
        case Entity o:
          var res = o.Append(layer);
          if (res.IsValid)
          {
            // handle display - fallback to rendermaterial if no displaystyle exists
            if (obj[@"displayStyle"] is not Base display)
            {
              display = obj[@"renderMaterial"] as Base;
            }

            if (display != null)
            {
              SetStyle(display, o, LineTypeDictionary);
            }

            // add property sets if this is Civil3D
#if CIVIL
            if (obj["propertySets"] is Base propertySetsBase)
            {
              List<Dictionary<string, object>> propertySets = new();
              foreach (object baseObj in propertySetsBase.GetMembers(DynamicBaseMemberType.Dynamic).Values)
              {
                if (baseObj is Dictionary<string, object> propertySet)
                {
                  propertySets.Add(propertySet);
                }
              }

              try
              {
                o.SetPropertySets(Doc, propertySets);
              }
              catch (Exception e) when (!e.IsFatal())
              {
                SpeckleLog.Logger.Error(e, "Could not set property sets: {exceptionMessage}");
                appObj.Log.Add($"Could not attach property sets: {e.Message}");
              }
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
            {
              parent.Update(createdId: res.Handle.ToString());
            }
            else
            {
              appObj.Update(createdId: res.Handle.ToString());
            }

            bakedCount++;
          }
          else
          {
            var bakeMessage = $"Could not bake to document.";
            if (parent != null)
            {
              parent.Update(logItem: $"fallback {appObj.applicationId}: {bakeMessage}");
            }
            else
            {
              appObj.Update(logItem: bakeMessage);
            }

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
      {
        parent.Update(logItem: $"fallback {appObj.applicationId}: could not bake object");
      }
      else
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not bake object");
      }
    }
    else
    {
      // remove existing objects if they exist
      if (remove)
      {
        foreach (var objId in toRemove)
        {
          DBObject objToRemove = tr.GetObject(objId, OpenMode.ForWrite);

          if (!objToRemove.IsErased)
          {
            objToRemove.Erase();
          }
        }
        appObj.Status = toRemove.Count > 0 ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      }
    }
  }

  private bool DeleteBlocksWithPrefix(string prefix, Transaction tr, out List<string> failedBlocks)
  {
    failedBlocks = new List<string>();
    bool success = true;
    if (tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) is BlockTable blockTable)
    {
      foreach (ObjectId blockId in blockTable)
      {
        if (tr.GetObject(blockId, OpenMode.ForRead) is BlockTableRecord block)
        {
          if (block.Name.StartsWith(prefix) && !block.IsErased)
          {
            try
            {
              block.UpgradeOpen();
              block.Erase();
            }
            catch (Exception e) when (!e.IsFatal())
            {
              SpeckleLog.Logger.Error(
                e,
                $"Could not delete existing block of name {block.Name} before creating new blocks with prefix {prefix}"
              );
              failedBlocks.Add(block.Name);
              success = false;
            }
          }
        }
      }
    }
    return success;
  }
}
