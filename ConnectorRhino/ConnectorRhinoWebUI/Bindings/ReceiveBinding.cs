using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Models.Card;
using DUI3.Operations;
using DUI3.Settings;
using DUI3.Utils;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

using ICancelable = DUI3.Operations.ICancelable;

namespace ConnectorRhinoWebUI.Bindings;

public class ReceiveBinding : IReceiveBinding, ICancelable
{
  public string Name { get; set; } = "receiveBinding";
  public IBridge Parent { get; set; }

  private readonly DocumentModelStore _store;

  private RhinoDoc Doc => RhinoDoc.ActiveDoc;

  public CancellationManager CancellationManager { get; } = new();

  public ReceiveBinding(DocumentModelStore store)
  {
    _store = store;
  }

  public void CancelReceive(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  public async void Receive(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get receiver card
      ReceiverModelCard modelCard = _store.GetModelById(modelCardId) as ReceiverModelCard;

      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress() { Status = "Downloading" }
      );

      // 2 - Get commit object from server
      Base commitObject = await Operations.GetCommitBase(Parent, modelCard, cts.Token).ConfigureAwait(true);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 3 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(Doc, "Rhino7");

      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress() { Status = "Parsing structure" }
      );

      var objs = commitObject.TraverseWithCollectionPath(
        obj => obj is not Collection && converter.CanConvertToNative(obj)
      );

      var convertableObjects = new List<(List<Collection> collectionPath, Base obj)>();
      var instancesAndInstanceDefinitions = new List<(List<Collection> collectionPath, IInstanceComponent obj)>();
      foreach (var (collectionPath, obj) in objs)
      {
        if (cts.IsCancellationRequested)
        {
          throw new OperationCanceledException(cts.Token);
        }

        if (obj is not Collection && converter.CanConvertToNative(obj))
        {
          convertableObjects.Add((collectionPath, obj));
        }

        if (obj is IInstanceComponent p)
        {
          instancesAndInstanceDefinitions.Add((collectionPath, p));
        }
      }

      var baseLayerName = $"Project {modelCard.ProjectName}: Model {modelCard.ModelName}";
      var convertedIds = BakeObjects(
        convertableObjects,
        instancesAndInstanceDefinitions,
        baseLayerName,
        modelCardId,
        cts,
        converter
      );

      var receiveResult = new ReceiveResult() { BakedObjectIds = convertedIds, Display = true };

      ReceiveBindingUiCommands.SetModelConversionResult(Parent, modelCardId, receiveResult);

      // 7 - Redraw the view to render baked objects
      Doc.Views.Redraw();
    }
    catch (Exception e) when (!e.IsFatal())
    {
      if (e is OperationCanceledException) // We do not want to display an error, we just stop sending.
      {
        return;
      }

      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId, e); // NOTE: should be a shard UI binding command
    }
  }

  private List<string> BakeObjects(
    List<(List<Collection> collectionPath, Base obj)> objects,
    List<(List<Collection> collectionPath, IInstanceComponent obj)> instancesAndInstanceDefinitions,
    string baseLayerName,
    string modelCardId,
    CancellationTokenSource cts,
    ISpeckleConverter converter
  )
  {
    var rootLayerName = baseLayerName;

#pragma warning disable CS0618
    var rootLayerIndex = Doc.Layers.Find(rootLayerName, true);
#pragma warning restore CS0618

    // Cleanup blocks/definitions/instances before layers
    foreach (var definition in Doc.InstanceDefinitions)
    {
      if (!definition.IsDeleted && definition.Name.Contains(rootLayerName))
      {
        Doc.InstanceDefinitions.Delete(definition.Index, true, false);
      }
    }

    // Cleans up any previously received objects
    if (rootLayerIndex >= 0)
    {
      foreach (var layer in RhinoDoc.ActiveDoc.Layers[rootLayerIndex].GetChildren())
      {
        RhinoDoc.ActiveDoc.Layers.Purge(layer.Index, false);
      }
    }

    var cache = new Dictionary<string, int>();
    rootLayerIndex = Doc.Layers.Add(new Layer() { Name = rootLayerName });
    cache.Add(rootLayerName, rootLayerIndex);

    var newObjectIds = new List<string>();
    var applicationIdMap = new Dictionary<string, string>();
    var count = 0;

    // Stage 1: Raw atomic objects conversion
    foreach (var (path, baseObj) in objects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      var layerIndex = GetAndCreateLayerFromPath(path, rootLayerName, cache);

      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress() { Status = "Converting & creating objects", Progress = (double)++count / objects.Count }
      );

      // Skips instances (blocks) as they are handled in the second stage
      if (baseObj is InstanceProxy)
      {
        continue;
      }

      var converted = converter.ConvertToNative(baseObj);
      if (converted is GeometryBase newObject)
      {
        var newObjectGuid = Doc.Objects.Add(newObject, new ObjectAttributes() { LayerIndex = layerIndex });
        newObjectIds.Add(newObjectGuid.ToString());
        if (baseObj.applicationId != null)
        {
          applicationIdMap[baseObj.applicationId] = newObjectGuid.ToString();
        }
      }
    }

    // Stage 2: Instances and Instance Definitions
    var sortedList = instancesAndInstanceDefinitions
      .OrderByDescending(x => x.obj.MaxDepth) // Sort by max depth, so we start baking from the deepest element first
      .ThenBy(x => x.obj is InstanceDefinitionProxy ? 0 : 1) // Ensure we bake the deepest definition first, then any instances that depend on it
      .ToList();
    var definitionIdAndApplicationIdMap = new Dictionary<string, int>();

    foreach (var (path, instanceOrDefinition) in sortedList)
    {
      if (instanceOrDefinition is InstanceDefinitionProxy definitionProxy)
      {
        var currentApplicationObjectsIds = definitionProxy.Objects
          .Select(x => applicationIdMap.TryGetValue(x, out string value) ? value : null)
          .Where(x => x is not null)
          .ToList();

        var definitionGeometryList = new List<GeometryBase>();
        var attributes = new List<ObjectAttributes>();

        foreach (var id in currentApplicationObjectsIds)
        {
          var docObject = Doc.Objects.FindId(new Guid(id));
          if (docObject is InstanceObject inst)
          {
            // DO something else
            definitionGeometryList.Add(docObject.Geometry); // Seems ok re if this is ok for nested blocks A: NO ITS NOT
            attributes.Add(docObject.Attributes);
          }
          else
          {
            definitionGeometryList.Add(docObject.Geometry); // Seems ok re if this is ok for nested blocks A: NO ITS NOT
            attributes.Add(docObject.Attributes);
          }
        }

        // TODO: Currently we're relying on the definition name for identification if it's coming from speckle and from which model; could we do something else?
        var defName = baseLayerName + " " + definitionProxy.applicationId; // TODO: something nicer? We might need to clean them later on
        var defIndex = Doc.InstanceDefinitions.Add(
          defName,
          "No description", // TODO: perhaps bring it along from source? We'd need to look at ACAD first
          Point3d.Origin,
          definitionGeometryList,
          attributes
        );

        // TODO: check on defIndex -1, means we haven't created anything - this is most likely an recoverable error at this stage
        if (defIndex == -1)
        {
          var x = "break";
        }

        if (definitionProxy.applicationId != null)
        {
          definitionIdAndApplicationIdMap[definitionProxy.applicationId] = defIndex;
        }

        Doc.Objects.Delete(currentApplicationObjectsIds.Select(stringId => new Guid(stringId)), false);
        newObjectIds.RemoveAll(id => currentApplicationObjectsIds.Contains(id));
      }
      if (
        instanceOrDefinition is InstanceProxy instanceProxy
        && definitionIdAndApplicationIdMap.TryGetValue(instanceProxy.DefinitionId, out int index)
      )
      {
        var transform = RhinoInstanceUnpacker.MatrixToTransform(instanceProxy.Transform);
        var layerIndex = GetAndCreateLayerFromPath(path, rootLayerName, cache);
        var id = Doc.Objects.AddInstanceObject(index, transform, new ObjectAttributes() { LayerIndex = layerIndex });
        if (instanceProxy.applicationId != null)
        {
          applicationIdMap[instanceProxy.applicationId] = id.ToString();
        }
        newObjectIds.Add(id.ToString());
      }
    }

    return newObjectIds;
  }

  private int GetAndCreateLayerFromPath(
    List<Collection> collectionPath,
    string baseLayerName,
    Dictionary<string, int> cache
  )
  {
    var fullLayerName = string.Join("::", collectionPath.Select(col => col.name));
#pragma warning disable CA1854
    if (cache.ContainsKey(fullLayerName)) // Do not use try get value here, it defaults to zero rather than -1 (which is what we need)
#pragma warning restore CA1854
    {
      return cache[fullLayerName];
    }

    var currentLayerName = baseLayerName;
    var previousLayer = Doc.Layers.FindName(currentLayerName);
    foreach (var collection in collectionPath)
    {
      currentLayerName = baseLayerName + Layer.PathSeparator + collection.name;
      currentLayerName = currentLayerName.Replace("{", "").Replace("}", ""); // Rhino specific cleanup for gh (see RemoveInvalidRhinoChars)
      if (cache.TryGetValue(currentLayerName, out int value))
      {
        previousLayer = Doc.Layers.FindIndex(value);
        continue;
      }
      var cleanNewLayerName = collection.name.Replace("{", "").Replace("}", "");
      var newLayer = new Layer
      {
        Name = cleanNewLayerName,
        ParentLayerId = previousLayer.Id,
        Color = collection["layerColor"] is long layerColor ? Color.FromArgb((int)layerColor) : Color.Black,
        PlotColor = collection["plotColor"] is long plotColor ? Color.FromArgb((int)plotColor) : Color.Black,
        PlotWeight = collection["plotWeight"] is double plotWeight ? plotWeight : 1
        // TODO: Render Material (probably needs conversion rethinking).
      };

      var index = Doc.Layers.Add(newLayer);
      cache.Add(currentLayerName, index);
      previousLayer = Doc.Layers.FindIndex(index); // note we need to get the correct id out, hence why we're double calling this
    }
    return previousLayer.Index;
  }

  public List<CardSetting> GetReceiveSettings() =>
    new()
    {
      new()
      {
        Id = "mergeCoplanarFaces",
        Title = "Merge Coplanar Faces",
        Value = true,
        Type = "boolean"
      },
      new()
      {
        Id = "receiveMode",
        Title = "Receive Mode",
        Value = "Update",
        Type = "string",
        Enum = new List<string>() { "Update", "Create", "Ignore" }
      }
    };
}
