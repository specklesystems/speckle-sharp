using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
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
      }

      var baseLayerName = $"Project {modelCard.ProjectName}: Model {modelCard.ModelName}";
      var convertedIds = BakeObjects(convertableObjects, baseLayerName, modelCardId, cts, converter);

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
    string baseLayerName,
    string modelCardId,
    CancellationTokenSource cts,
    ISpeckleConverter converter
  )
  {
    var rootLayerName = baseLayerName;

#pragma warning disable CS0618
    // NOTE: I could not get the non-obsolete version to work as intended
    var rootLayerIndex = Doc.Layers.Find(rootLayerName, true);
#pragma warning restore CS0618

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
    var count = 0;
    foreach (var (path, baseObj) in objects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }
      var fullLayerName = string.Join("::", path);
      var layerIndex = -1;
      if (cache.ContainsKey(fullLayerName))
      {
        layerIndex = cache[fullLayerName];
      }

      if (layerIndex == -1)
      {
        layerIndex = GetAndCreateLayerFromPath(path, rootLayerName, cache);
      }

      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress() { Status = "Converting & creating objects", Progress = (double)++count / objects.Count }
      );

      var converted = converter.ConvertToNative(baseObj);
      if (converted is GeometryBase newObject)
      {
        var newObjectGuid = Doc.Objects.Add(newObject, new ObjectAttributes() { LayerIndex = layerIndex });
        newObjectIds.Add(newObjectGuid.ToString());
      }
      // else something weird happened? a block maybe? also, blocks are treated like $$$ now tbh so i won't dive into them
    }

    return newObjectIds;
  }

  private int GetAndCreateLayerFromPath(
    List<Collection> collectionPath,
    string baseLayerName,
    Dictionary<string, int> cache
  )
  {
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
