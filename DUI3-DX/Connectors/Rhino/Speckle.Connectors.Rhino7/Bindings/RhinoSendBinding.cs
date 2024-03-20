using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Rhino;
using Rhino.DocObjects;
using Speckle.Autofac.DependencyInjection;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.Rhino7.Filters;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using ICancelable = System.Reactive.Disposables.ICancelable;

namespace Speckle.Connectors.Rhino7.Bindings;

public sealed class RhinoSendBinding : ISendBinding, ICancelable
{
  public string Name { get; } = "sendBinding";
  public SendBindingUICommands Commands { get; }
  public IBridge Parent { get; set; }

  private readonly DocumentModelStore _store;
  private readonly RhinoIdleManager _idleManager;
  private readonly RhinoSettings _rhinoSettings;
  private readonly RhinoContext _rhinoContext;
  private readonly IBasicConnectorBinding _basicConnectorBinding;
  private readonly ISpeckleConverterToSpeckle _toSpeckleConverter;
  private readonly IScopedFactory<ISpeckleConverterToSpeckle> _speckleConverterToSpeckleFactory;

  public CancellationManager CancellationManager { get; } = new();

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  /// <summary>
  /// Keeps track of previously converted objects as a dictionary of (applicationId, object reference).
  /// </summary>
  private readonly Dictionary<string, ObjectReference> _convertedObjectReferences = new();

  public RhinoSendBinding(
    DocumentModelStore store,
    RhinoIdleManager idleManager,
    RhinoSettings rhinoSettings,
    IBridge parent,
    IBasicConnectorBinding basicConnectorBinding,
    IScopedFactory<ISpeckleConverterToSpeckle> speckleConverterToSpeckleFactory,
    RhinoContext rhinoContext
  )
  {
    _store = store;
    _idleManager = idleManager;
    _rhinoSettings = rhinoSettings;
    _basicConnectorBinding = basicConnectorBinding;
    _speckleConverterToSpeckleFactory = speckleConverterToSpeckleFactory;
    _rhinoContext = rhinoContext;
    _toSpeckleConverter = _speckleConverterToSpeckleFactory.ResolveScopedInstance();

    Parent = parent;
    Commands = new SendBindingUICommands(parent);

    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      Commands.RefreshSendFilters();
    };

    RhinoDoc.AddRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      _idleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      _idleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      ChangedObjectIds.Add(e.OldRhinoObject.Id.ToString());
      _idleManager.SubscribeToIdle(RunExpirationChecks);
    };
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>
    {
      new RhinoEverythingFilter(),
      new RhinoSelectionFilter { IsDefault = true }
    };
  }

  public List<CardSetting> GetSendSettings()
  {
    return new List<CardSetting>
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

  [SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Being refactored on in parallel, muting this issue so CI can pass initially."
  )]
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
      // TODO: FETCHING ACCOUNTS BY ID ONLY IS UNSAFE
      Account account =
        AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == modelCard.AccountId)
        ?? throw new SpeckleAccountManagerException();

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

      var commitObject = ConvertObjects(rhinoObjects, modelCard, cts.Token);

      cts.Token.ThrowIfCancellationRequested();

      // 7 - Serialize and Send objects
      _basicConnectorBinding.Commands.SetModelProgress(modelCardId, new ModelCardProgress { Status = "Uploading..." });

      var transport = new ServerTransport(account, modelCard.ProjectId);
      var sendResult = await SendHelper.Send(commitObject, transport, true, null, cts.Token).ConfigureAwait(true);

      // Store the converted references in memory for future send operations, overwriting the existing values for the given application id.
      // foreach (var kvp in sendResult.convertedReferences)
      // {
      //   // TODO: Bug in here, we need to encapsulate cache not only by app id, but also by project id,
      //   // TODO: as otherwise we assume incorrectly that an object exists for a given project (e.g, send box to project 1, send same unchanged box to project 2)
      //   _convertedObjectReferences[kvp.Key + modelCard.ProjectId] = kvp.Value;
      // }
      // It's important to reset the model card's list of changed obj ids so as to ensure we accurately keep track of changes between send operations.
      // NOTE: ChangedObjectIds is currently JsonIgnored, but could actually be useful for highlighting changes in host app.
      //modelCard.ChangedObjectIds = new();

      _basicConnectorBinding.Commands.SetModelProgress(
        modelCardId,
        new ModelCardProgress { Status = "Linking version to model..." }
      );

      // 8 - Create the version (commit)
      var apiClient = new Client(account);
      string versionId = await apiClient
        .CommitCreate(
          new CommitCreateInput
          {
            streamId = modelCard.ProjectId,
            branchName = modelCard.ModelId,
            sourceApplication = "Rhino",
            objectId = sendResult.rootObjId
          },
          cts.Token
        )
        .ConfigureAwait(true);

      Commands.SetModelCreatedVersionId(modelCardId, versionId);
      apiClient.Dispose();
    }
    catch (OperationCanceledException)
    {
      return;
    }
    catch (Exception e) when (!e.IsFatal()) // All exceptions should be handled here if possible, otherwise we enter "crashing the host app" territory.
    {
      _basicConnectorBinding.Commands.SetModelError(modelCardId, e);
    }
  }

  public void CancelSend(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  private Base ConvertObjects(
    List<RhinoObject> rhinoObjects,
    SenderModelCard modelCard,
    CancellationToken cancellationToken
  )
  {
    ISpeckleConverterToSpeckle converter = _speckleConverterToSpeckleFactory.ResolveScopedInstance();

    var rootObjectCollection = new Collection { name = RhinoDoc.ActiveDoc.Name ?? "Unnamed document" };
    int count = 0;

    Dictionary<int, Collection> layerCollectionCache = new();
    // TODO: Handle blocks.
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // 1. get object layer
      var layer = RhinoDoc.ActiveDoc.Layers[rhinoObject.Attributes.LayerIndex];

      // 2. get or create a nested collection for it
      var collectionHost = GetHostObjectCollection(layerCollectionCache, layer, rootObjectCollection);
      var applicationId = rhinoObject.Id.ToString();

      // 3. get from cache or convert:
      // What we actually do here is check if the object has been previously converted AND has not changed.
      // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
      /*Base converted;
      if (
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
      }*/
      try
      {
        var converted = converter.Convert(rhinoObject);
        converted.applicationId = applicationId;

        // 4. add to host
        collectionHost.elements.Add(converted);
        _basicConnectorBinding.Commands.SetModelProgress(
          modelCard.ModelCardId,
          new ModelCardProgress { Status = "Converting", Progress = (double)++count / rhinoObjects.Count }
        );
      }
      catch (SpeckleConversionException e)
      {
        // DO something with the exception
        Console.WriteLine(e);
      }
      catch (NotSupportedException e)
      {
        // DO something with the exception
        Console.WriteLine(e);
      }

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
      Collection? childCollection = null;
      if (layerCollectionCache.TryGetValue(existingLayerIndex, out Collection? collection))
      {
        childCollection = collection;
      }
      else
      {
        childCollection = new Collection(layerName, "layer")
        {
          applicationId = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].Id.ToString()
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
    List<string> expiredSenderIds = new();

    foreach (var sender in senders)
    {
      bool isExpired = sender.SendFilter.CheckExpiry(ChangedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(sender.ModelCardId);
      }
    }

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  public void Dispose()
  {
    IsDisposed = true;
    _speckleConverterToSpeckleFactory.Dispose();
  }

  public bool IsDisposed { get; private set; }
}
