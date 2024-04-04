using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Models.Card;
using DUI3.Operations;
using DUI3.Settings;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorRhinoWebUI.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }
  private readonly DocumentModelStore _store;
  public CancellationManager CancellationManager { get; } = new();

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  /// <summary>
  /// Keeps track of previously converted objects as a dictionary of (applicationId, object reference).
  /// </summary>
  private readonly Dictionary<string, ObjectReference> _convertedObjectReferences = new();

  public SendBinding(DocumentModelStore store)
  {
    _store = store;

    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      SendBindingUiCommands.RefreshSendFilters(Parent);
    };

    RhinoDoc.AddRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      // var paths = BlockHierarchyPaths.GetBlockHierarchyPaths(e.Document);
      // var objectPaths = BlockHierarchyPaths.GetObjectHierarchyPaths(e.NewRhinoObject, e.Document);
      // var oldObjectPaths = BlockHierarchyPaths.GetObjectHierarchyPaths(e.OldRhinoObject, e.Document);
      // var path = RhinoObjectPath(e.NewRhinoObject);

      ChangedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      ChangedObjectIds.Add(e.OldRhinoObject.Id.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };
  }

  public List<string> RhinoObjectPath(RhinoObject rhinoObject)
  {
    List<string> path = new();
    var doc = RhinoDoc.ActiveDoc;
    // var objects = doc.Objects.ToList();
    // IEnumerable<InstanceObject> instances =
    //   RhinoDoc.ActiveDoc.Objects.Where(obj => obj is InstanceObject) as IEnumerable<InstanceObject>;
    // foreach (InstanceObject instance in instances)
    // {
    //   var definitionObjects = instance.InstanceDefinition.GetObjects();
    //   if (definitionObjects.Contains(rhinoObject)) { }
    // }
    return path;
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>()
    {
      new RhinoEverythingFilter(),
      new RhinoSelectionFilter() { IsDefault = true }
    };
  }

  public List<CardSetting> GetSendSettings()
  {
    return new List<CardSetting>()
    {
      new()
      {
        Id = "includeAttributes",
        Title = "Include Attributes",
        Value = true,
        Type = "boolean"
      },
    };
  }

  public async void Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model

      if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      // 2 - Check account exist
      Account account = Accounts.GetAccount(modelCard.AccountId);

      // 3 - Get elements to convert, throw early if nothing is selected
      List<RhinoObject> rhinoObjects = modelCard.SendFilter
        .GetObjectIds()
        .Select(id => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id)))
        .Where(obj => obj != null)
        .ToList();

      if (rhinoObjects.Count == 0)
      {
        throw new InvalidOperationException("No objects were found. Please update your send filter!");
      }

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(RhinoDoc.ActiveDoc, "Rhino7");

      // 5 - Convert objects
      Base commitObject = ConvertObjects(rhinoObjects, converter, modelCard, cts);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 7 - Serialize and Send objects
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress { Status = "Uploading..." }
      );
      var transport = new ServerTransport(account, modelCard.ProjectId);
      var sendResult = await SendHelper.Send(commitObject, transport, true, null, cts.Token).ConfigureAwait(true);

      // Store the converted references in memory for future send operations, overwriting the existing values for the given application id.
      foreach (var kvp in sendResult.convertedReferences)
      {
        // NOTE: Object cache needs to be encapsulated by project id, as objects are unique by project. Otherwise we
        // can assume incorrectly that an object exists for a given project when actually it is not (e.g., send box to
        // a model in project 1, send same unchanged box to a different model in project 2).
        _convertedObjectReferences[kvp.Key + modelCard.ProjectId] = kvp.Value;
      }
      // It's important to reset the model card's list of changed obj ids so as to ensure we accurately keep track of changes between send operations.
      // NOTE: ChangedObjectIds is currently JsonIgnored, but could actually be useful for highlighting changes in host app.
      modelCard.ChangedObjectIds = new();

      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress { Status = "Linking version to model..." }
      );

      // 8 - Create the version (commit)
      var apiClient = new Client(account);
      string versionId = await apiClient
        .CommitCreate(
          new CommitCreateInput()
          {
            streamId = modelCard.ProjectId,
            branchName = modelCard.ModelId,
            sourceApplication = "Rhino",
            objectId = sendResult.rootObjId
          },
          cts.Token
        )
        .ConfigureAwait(true);

      SendBindingUiCommands.SetModelCreatedVersionId(Parent, modelCardId, versionId);
      apiClient.Dispose();
    }
    catch (Exception e) when (!e.IsFatal()) // All exceptions should be handled here if possible, otherwise we enter "crashing the host app" territory.
    {
      if (e is OperationCanceledException) // We do not want to display an error, we just stop sending.
      {
        return;
      }

      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId, e);
    }
  }

  public void CancelSend(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  private Base ConvertObjects(
    List<RhinoObject> rhinoObjects,
    ISpeckleConverter converter,
    SenderModelCard modelCard,
    CancellationTokenSource cts
  )
  {
    var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Dictionary<int, Collection> layerCollectionCache = new();

    var unpacker = new RhinoInstanceUnpacker();

    unpacker.Unpack(rhinoObjects);

    rootObjectCollection["instanceDefs"] = unpacker.DefinitionProxies.Select(kvp => kvp.Value);
    rootObjectCollection["groups"] = "something";
    rootObjectCollection["hostingDefs"] = "somethingelse";

    foreach (RhinoObject rhinoObject in unpacker.FlatAtomicObjects.Values)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 1. get object layer
      var layer = RhinoDoc.ActiveDoc.Layers[rhinoObject.Attributes.LayerIndex];

      // 2. get or create a nested collection for it
      var collectionHost = GetHostObjectCollection(layerCollectionCache, layer, rootObjectCollection);
      var applicationId = rhinoObject.Id.ToString();

      // 3. get from cache or convert:
      // What we actually do here is check if the object has been previously converted AND has not changed.
      // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
      Base converted;

      if (rhinoObject is InstanceObject) // Special case for blocks, bypass cache; TODO: we need to think about cache invalidation of sub objects (are the proxy types enough?)
      {
        converted = unpacker.InstanceProxies[applicationId];
      }
      else if (
        !modelCard.ChangedObjectIds.Contains(applicationId)
        && _convertedObjectReferences.TryGetValue(applicationId + modelCard.ProjectId, out ObjectReference value)
      )
      {
        converted = value;
      }
      else
      {
        converted = converter.ConvertToSpeckle(rhinoObject);
        converted.applicationId = applicationId;
      }

      // 4. add to host
      collectionHost.elements.Add(converted);
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCard.ModelCardId,
        new ModelCardProgress() { Status = "Converting", Progress = (double)++count / rhinoObjects.Count }
      );

      // NOTE: useful for testing ui states, pls keep for now so we can easily uncomment
      // Thread.Sleep(550);
    }

    // 5. profit
    return rootObjectCollection;
  }

  /// <summary>
  /// Returns the host collection based on the provided layer. If it's not found, it will be created and hosted within the the rootObjectCollection.
  /// </summary>
  /// <param name="layerCollectionCache"></param>
  /// <param name="layer"></param>
  /// <param name="rootObjectCollection"></param>
  /// <returns></returns>
  private Collection GetHostObjectCollection(
    Dictionary<int, Collection> layerCollectionCache,
    Layer layer,
    Collection rootObjectCollection
  )
  {
    if (layerCollectionCache.TryGetValue(layer.Index, out Collection value))
    {
      return value;
    }

    var names = layer.FullPath.Split(new[] { Layer.PathSeparator }, StringSplitOptions.None);
    var path = names[0];
    var index = 0;
    var previousCollection = rootObjectCollection;
    foreach (var layerName in names)
    {
      var existingLayerIndex = RhinoDoc.ActiveDoc.Layers.FindByFullPath(path, -1);
      Collection childCollection = null;
      if (layerCollectionCache.ContainsKey(existingLayerIndex))
      {
        childCollection = layerCollectionCache[existingLayerIndex];
      }
      else
      {
        // TODO: Ask claire re display styles; think about making an actual layer class for autocad & rh?
        childCollection = new Collection(layerName, "layer")
        {
          applicationId = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].Id.ToString(),
          ["layerColor"] = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].Color,
          ["plotColor"] = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].PlotColor,
          ["plotWeight"] = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].PlotWeight,
          ["renderMaterial"] = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].RenderMaterial
        };

        previousCollection.elements.Add(childCollection);
        layerCollectionCache[existingLayerIndex] = childCollection;
      }

      previousCollection = childCollection;

      if (index < names.Length - 1)
      {
        path += Layer.PathSeparator + names[index + 1];
      }
      index++;
    }

    layerCollectionCache[layer.Index] = previousCollection;
    return previousCollection;
  }

  /// <summary>
  /// Checks if any sender model cards contain any of the changed objects. If so, also updates the changed objects hashset for each model card - this last part is important for on send change detection.
  /// </summary>
  private void RunExpirationChecks()
  {
    List<SenderModelCard> senders = _store.GetSenders();
    string[] objectIdsList = ChangedObjectIds.ToArray();
    List<string> expiredSenderIds = new();

    foreach (string id in objectIdsList)
    {
      var obj = RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id));
      if (obj is not null)
      {
        var oldObjectPaths = BlockHierarchyPaths.GetObjectHierarchyPaths(obj, RhinoDoc.ActiveDoc);
      }
    }

    foreach (SenderModelCard modelCard in senders)
    {
      var intersection = modelCard.SendFilter.GetObjectIds().Intersect(objectIdsList).ToList();
      bool isExpired = intersection.Any();
      if (isExpired)
      {
        expiredSenderIds.Add(modelCard.ModelCardId);
        modelCard.ChangedObjectIds.UnionWith(intersection);
      }
    }
    SendBindingUiCommands.SetModelsExpired(Parent, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }
}
