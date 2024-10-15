using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
using Speckle.Core.Transports;
using static Speckle.ConnectorAutocadCivil.Utils;

#if ADVANCESTEEL
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI;

public partial class ConnectorBindingsAutocad : ConnectorBindings
{
  public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
  {
    var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);

    var streamId = state.StreamId;
    var client = state.Client;
    progress.Report = new ProgressReport();

    if (state.Filter != null)
    {
      state.SelectedObjectIds = GetObjectsFromFilter(state.Filter, converter);
    }

    // remove deleted object ids
    var deletedElements = new List<string>();
    foreach (var selectedId in state.SelectedObjectIds)
    {
      if (GetHandle(selectedId, out Handle handle))
      {
        if (Doc.Database.TryGetObjectId(handle, out ObjectId id))
        {
          if (id.IsErased || id.IsNull)
          {
            deletedElements.Add(selectedId);
          }
        }
      }
    }

    state.SelectedObjectIds = state.SelectedObjectIds.Where(o => !deletedElements.Contains(o)).ToList();

    if (state.SelectedObjectIds.Count == 0)
    {
      throw new InvalidOperationException(
        "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."
      );
    }

    var modelName = $"{AppName} Model";
    var commitObject = new Collection(modelName, modelName.ToLower());
    commitObject["units"] = GetUnits(Doc); // TODO: check whether commits base needs units attached

    int convertedCount = 0;

    // invoke conversions on the main thread via control
    try
    {
      if (Control.InvokeRequired)
      {
        Control.Invoke(
          new Action(() => ConvertSendCommit(commitObject, converter, state, progress, ref convertedCount)),
          new object[] { }
        );
      }
      else
      {
        ConvertSendCommit(commitObject, converter, state, progress, ref convertedCount);
      }

      progress.Report.Merge(converter.Report);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      progress.Report.LogOperationError(ex);
    }

    if (convertedCount == 0)
    {
      throw new SpeckleException("Zero objects converted successfully. Send stopped.");
    }

    progress.CancellationToken.ThrowIfCancellationRequested();

    var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

    var commitObjId = await Operations.Send(
      commitObject,
      progress.CancellationToken,
      transports,
      onProgressAction: progress.Update,
      onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
      disposeTransports: true
    );

    progress.CancellationToken.ThrowIfCancellationRequested();

    var actualCommit = new CommitCreateInput
    {
      streamId = streamId,
      objectId = commitObjId,
      branchName = state.BranchName,
      message = state.CommitMessage ?? $"Pushed {convertedCount} elements from {AppName}.",
      sourceApplication = VersionedAppName
    };

    if (state.PreviousCommitId != null)
    {
      actualCommit.parents = new List<string>() { state.PreviousCommitId };
    }

    var commitId = await ConnectorHelpers.CreateCommit(client, actualCommit, progress.CancellationToken);
    return commitId;
  }

  delegate void SendingDelegate(
    Collection commitObject,
    ISpeckleConverter converter,
    StreamState state,
    ProgressViewModel progress,
    ref int convertedCount
  );

  private void ConvertSendCommit(
    Collection commitObject,
    ISpeckleConverter converter,
    StreamState state,
    ProgressViewModel progress,
    ref int convertedCount
  )
  {
    using (DocumentLock acLckDoc = Doc.LockDocument())
    {
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        // set the context doc for conversion - this is set inside the transaction loop because the converter retrieves this transaction for all db editing when the context doc is set!
        converter.SetContextDocument(Doc);

        // set converter settings as tuples (setting slug, setting selection)
        var settings = new Dictionary<string, string>();
        CurrentSettings = state.Settings;
        foreach (var setting in state.Settings)
        {
          settings.Add(setting.Slug, setting.Selection);
        }

        converter.SetConverterSettings(settings);

        var conversionProgressDict = new ConcurrentDictionary<string, int>();
        conversionProgressDict["Conversion"] = 0;

        // add applicationID xdata before send
        if (!ApplicationIdManager.AddApplicationIdXDataToDoc(Doc, tr))
        {
          progress.Report.LogOperationError(new Exception("Could not create document application id reg table"));
          return;
        }

        // get the hash of the file name to create a more unique application id
        var fileNameHash = GetDocumentId();

        string servicedApplication = converter.GetServicedApplications().First();

        // store converted commit objects and layers by layer paths
        var commitLayerObjects = new Dictionary<string, List<Base>>();
        var commitCollections = new Dictionary<string, Collection>();

        // track object types for mixpanel logging
        Dictionary<string, int> typeCountDict = new();

        foreach (var autocadObjectHandle in state.SelectedObjectIds)
        {
          // handle user cancellation
          if (progress.CancellationToken.IsCancellationRequested)
          {
            return;
          }

          // get the db object from id
          DBObject obj = null;
          string layer = null;
          string applicationId = null;
          if (GetHandle(autocadObjectHandle, out Handle hn))
          {
            obj = hn.GetObject(tr, out string type, out layer, out applicationId);
          }
          else
          {
            progress.Report.LogOperationError(new Exception($"Failed to find doc object ${autocadObjectHandle}."));
            continue;
          }

          // log selection object type
          var objectType = obj.GetType().ToString();
          typeCountDict.TryGetValue(objectType, out var currentCount);
          typeCountDict[objectType] = ++currentCount;

          // create applicationobject for reporting
          Base converted = null;
          var descriptor = ObjectDescriptor(obj);
          ApplicationObject reportObj = new(autocadObjectHandle, descriptor) { applicationId = autocadObjectHandle };

          if (!converter.CanConvertToSpeckle(obj))
          {
#if ADVANCESTEEL
            UpdateASObject(reportObj, obj);
#endif
            reportObj.Update(
              status: ApplicationObject.State.Skipped,
              logItem: $"Sending this object type is not supported in {Utils.AppName}"
            );
            progress.Report.Log(reportObj);
            continue;
          }

          try
          {
            // convert obj
            converter.Report.Log(reportObj); // Log object so converter can access
            converted = converter.ConvertToSpeckle(obj);
            if (converted == null)
            {
              reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
              progress.Report.Log(reportObj);
              continue;
            }

            /* TODO: adding the extension dictionary / xdata per object
            foreach (var key in obj.ExtensionDictionary)
              converted[key] = obj.ExtensionDictionary.GetUserString(key);
            */

#if CIVIL
            // add property sets if this is Civil3D
            if (obj.TryGetPropertySets(tr, out Base propertySets))
            {
              converted["propertySets"] = propertySets;
            }
#endif
            // handle converted object layer
            if (commitLayerObjects.ContainsKey(layer))
            {
              commitLayerObjects[layer].Add(converted);
            }
            else
            {
              commitLayerObjects.Add(layer, new List<Base> { converted });
            }

            // set application id
            #region backwards compatibility
            // this is just to overwrite old files with objects that have the unappended autocad native application id
            bool isOldApplicationId(string appId)
            {
              if (string.IsNullOrEmpty(appId))
              {
                return false;
              }

              return appId.Length == 5;
            }
            if (isOldApplicationId(applicationId))
            {
              ApplicationIdManager.SetObjectCustomApplicationId(
                obj,
                autocadObjectHandle,
                out applicationId,
                fileNameHash
              );
            }
            #endregion

            if (applicationId == null) // this object didn't have an xdata appId field
            {
              if (
                !ApplicationIdManager.SetObjectCustomApplicationId(
                  obj,
                  autocadObjectHandle,
                  out applicationId,
                  fileNameHash
                )
              )
              {
                reportObj.Log.Add("Could not set application id xdata");
              }
            }
            converted.applicationId = applicationId;

            // update progress
            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);

            // log report object
            reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {converted.GetType().Name}");
            progress.Report.Log(reportObj);

            convertedCount++;
          }
          catch (Exception e) when (!e.IsFatal())
          {
            SpeckleLog.Logger.Error(e, $"Failed object conversion");
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
            progress.Report.Log(reportObj);
            continue;
          }
        }

        #region layer handling
        // get the layer table
        var layerTable = (LayerTable)tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead);

        // convert layers as collections and attach all layer objects
        foreach (var layerPath in commitLayerObjects.Keys)
        {
          if (commitCollections.ContainsKey(layerPath))
          {
            commitCollections[layerPath].elements = commitLayerObjects[layerPath];
          }
          else
          {
            var layerRecord = (LayerTableRecord)tr.GetObject(layerTable[layerPath], OpenMode.ForRead);
            if (converter.ConvertToSpeckle(layerRecord) is Collection collection)
            {
              collection.elements = commitLayerObjects[layerPath];
              commitCollections.Add(layerPath, collection);
            }
          }
        }

        // attach collections to the base commit
        foreach (var collection in commitCollections)
        {
          commitObject.elements.Add(collection.Value);
        }

        #endregion

        // track the object type counts as an event before we try to send
        // this will tell us the composition of a commit the user is trying to convert and send, even if it's not successfully converted or sent
        // we are capped at 255 properties for mixpanel events, so we need to check dict entries
        var typeCountList = typeCountDict
          .Select(o => new { TypeName = o.Key, Count = o.Value })
          .OrderBy(pair => pair.Count)
          .Reverse()
          .Take(200);

        Analytics.TrackEvent(
          Analytics.Events.ConvertToSpeckle,
          new Dictionary<string, object>() { { "typeCount", typeCountList } }
        );

        tr.Commit();
      }
    }
  }

#if ADVANCESTEEL
  private void UpdateASObject(ApplicationObject applicationObject, DBObject obj)
  {
    if (!CheckAdvanceSteelObject(obj))
    {
      return;
    }

    ASFilerObject filerObject = GetFilerObjectByEntity<ASFilerObject>(obj);
    if (filerObject != null)
    {
      applicationObject.Update(descriptor: filerObject.GetType().Name);
    }
  }
#endif
}
