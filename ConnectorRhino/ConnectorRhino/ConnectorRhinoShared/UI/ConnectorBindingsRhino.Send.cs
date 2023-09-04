using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
  public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
  {
    var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
    converter.SetContextDocument(Doc);

    // set converter settings
    CurrentSettings = state.Settings;
    var settings = GetSettingsDict(CurrentSettings);
    converter.SetConverterSettings(settings);

    var streamId = state.StreamId;
    var client = state.Client;

    int objCount = 0;

    state.SelectedObjectIds = GetObjectsFromFilter(state.Filter);
    var commitObject = converter.ConvertToSpeckle(Doc) as Collection; // create a collection base obj

    if (state.SelectedObjectIds.Count == 0)
      throw new InvalidOperationException(
        "Zero objects selected: Please select some objects, or check that your filter can actually select something."
      );

    progress.Report = new ProgressReport();
    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 0;

    progress.Max = state.SelectedObjectIds.Count;

    // store converted commit objects and layers by layer paths
    var commitLayerObjects = new Dictionary<string, List<Base>>();
    var commitLayers = new Dictionary<string, Layer>();
    var commitCollections = new Dictionary<string, Collection>();

    // convert all commit objs
    foreach (var selectedId in state.SelectedObjectIds)
    {
      progress.CancellationToken.ThrowIfCancellationRequested();

      Base converted = null;
      string applicationId = null;
      var reportObj = new ApplicationObject(selectedId, "Unknown");
      if (Utils.FindObjectBySelectedId(Doc, selectedId, out object obj, out string descriptor))
      {
        // create applicationObject
        reportObj = new ApplicationObject(selectedId, descriptor);
        converter.Report.Log(reportObj); // Log object so converter can access
        switch (obj)
        {
          case RhinoObject o:
            applicationId = o.Attributes.GetUserString(ApplicationIdKey) ?? selectedId;
            if (!converter.CanConvertToSpeckle(o))
            {
              reportObj.Update(
                status: ApplicationObject.State.Skipped,
                logItem: "Sending this object type is not supported in Rhino"
              );
              progress.Report.Log(reportObj);
              continue;
            }

            converted = converter.ConvertToSpeckle(o);

            if (converted != null)
            {
              var objectLayer = Doc.Layers[o.Attributes.LayerIndex];
              if (commitLayerObjects.ContainsKey(objectLayer.FullPath))
                commitLayerObjects[objectLayer.FullPath].Add(converted);
              else
                commitLayerObjects.Add(objectLayer.FullPath, new List<Base> { converted });
              if (!commitLayers.ContainsKey(objectLayer.FullPath))
                commitLayers.Add(objectLayer.FullPath, objectLayer);
            }
            break;
          case Layer o:
            applicationId = o.GetUserString(ApplicationIdKey) ?? selectedId;
            converted = converter.ConvertToSpeckle(o);
            if (converted is Collection layerCollection && !commitLayers.ContainsKey(o.FullPath))
            {
              commitLayers.Add(o.FullPath, o);
              commitCollections.Add(o.FullPath, layerCollection);
            }
            break;
          case ViewInfo o:
            converted = converter.ConvertToSpeckle(o);
            if (converted != null)
              commitObject.elements.Add(converted);
            break;
        }
      }
      else
      {
        progress.Report.LogOperationError(new Exception($"Failed to find doc object ${selectedId}."));
        continue;
      }

      if (converted == null)
      {
        reportObj.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null");
        progress.Report.Log(reportObj);
        continue;
      }

      conversionProgressDict["Conversion"]++;
      progress.Update(conversionProgressDict);

      // set application ids, also set for speckle schema base object if it exists
      converted.applicationId = applicationId;
      if (converted["@SpeckleSchema"] != null)
      {
        var newSchemaBase = converted["@SpeckleSchema"] as Base;
        newSchemaBase.applicationId = applicationId;
        converted["@SpeckleSchema"] = newSchemaBase;
      }

      // log report object
      reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {converted.speckle_type}");
      progress.Report.Log(reportObj);

      objCount++;
    }

    #region layer handling
    // convert layers as collections and attach all layer objects
    foreach (var layerPath in commitLayerObjects.Keys)
      if (commitCollections.ContainsKey(layerPath))
      {
        commitCollections[layerPath].elements = commitLayerObjects[layerPath];
      }
      else
      {
        var collection = converter.ConvertToSpeckle(commitLayers[layerPath]) as Collection;
        if (collection != null)
        {
          collection.elements = commitLayerObjects[layerPath];
          commitCollections.Add(layerPath, collection);
        }
      }

    // generate all parent paths of commit collections and create ordered list by depth descending
    var allPaths = new HashSet<string>();
    foreach (var key in commitLayers.Keys)
    {
      if (!allPaths.Contains(key))
        allPaths.Add(key);
      AddParent(commitLayers[key]);

      void AddParent(Layer childLayer)
      {
        var parentLayer = Doc.Layers.FindId(childLayer.ParentLayerId);
        if (parentLayer != null && !commitCollections.ContainsKey(parentLayer.FullPath))
        {
          var parentCollection = converter.ConvertToSpeckle(parentLayer) as Collection;
          if (parentCollection != null)
          {
            commitCollections.Add(parentLayer.FullPath, parentCollection);
            allPaths.Add(parentLayer.FullPath);
          }
          AddParent(parentLayer);
        }
      }
    }
    var orderedPaths = allPaths.OrderByDescending(path => path.Count(c => c == ':')).ToList(); // this ensures we attach children collections first

    // attach children collections to their parents and the base commit
    for (int i = 0; i < orderedPaths.Count; i++)
    {
      var path = orderedPaths[i];
      var collection = commitCollections[path];
      var parentIndex = path.LastIndexOf(Layer.PathSeparator);

      // if there is no parent, attach to base commit layer prop directly
      if (parentIndex == -1)
      {
        commitObject.elements.Add(collection);
        continue;
      }

      // get the parent collection, attach child, and update parent collection in commit collections
      var parentPath = path.Substring(0, parentIndex);
      var parent = commitCollections[parentPath];
      parent.elements.Add(commitCollections[path]);
      commitCollections[parentPath] = parent;
    }

    #endregion

    progress.Report.Merge(converter.Report);

    if (objCount == 0)
      throw new SpeckleException("Zero objects converted successfully. Send stopped.");

    progress.CancellationToken.ThrowIfCancellationRequested();

    progress.Max = objCount;

    var transports = new List<ITransport> { new ServerTransport(client.Account, streamId) };

    var objectId = await Operations.Send(
      commitObject,
      progress.CancellationToken,
      transports,
      onProgressAction: dict => progress.Update(dict),
      onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
      disposeTransports: true
    );

    progress.CancellationToken.ThrowIfCancellationRequested();

    var actualCommit = new CommitCreateInput
    {
      streamId = streamId,
      objectId = objectId,
      branchName = state.BranchName,
      message = state.CommitMessage ?? $"Sent {objCount} elements from Rhino.",
      sourceApplication = Utils.RhinoAppName
    };

    if (state.PreviousCommitId != null)
      actualCommit.parents = new List<string> { state.PreviousCommitId };

    var commitId = await ConnectorHelpers.CreateCommit(client, actualCommit, progress.CancellationToken);
    return commitId;
  }
}
