using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Rhino;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Core.Logging;
using ICancelable = System.Reactive.Disposables.ICancelable;
using System.Threading.Tasks;
using Speckle.Autofac.DependencyInjection;
using Rhino.DocObjects;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models;

namespace Speckle.Connectors.Rhino7.Bindings;

public sealed class RhinoSendBinding : ISendBinding, ICancelable
{
  public string Name { get; } = "sendBinding";
  public SendBindingUICommands Commands { get; }
  public IBridge Parent { get; set; }

  private readonly DocumentModelStore _store;
  private readonly RhinoIdleManager _idleManager;
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  private readonly List<ISendFilter> _sendFilters;
  private readonly SendOperation<RhinoObject> _sendOperation;
  private readonly CancellationManager _cancellationManager;
  private readonly RhinoSettings _rhinoSettings;

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
    IBridge parent,
    IEnumerable<ISendFilter> sendFilters,
    SendOperation<RhinoObject> sendOperation,
    IUnitOfWorkFactory unitOfWorkFactory,
    RhinoSettings rhinoSettings,
    CancellationManager cancellationManager
  )
  {
    _store = store;
    _idleManager = idleManager;
    _unitOfWorkFactory = unitOfWorkFactory;
    _sendOperation = sendOperation;
    _sendFilters = sendFilters.ToList();
    _rhinoSettings = rhinoSettings;
    _cancellationManager = cancellationManager;
    Parent = parent;
    Commands = new SendBindingUICommands(parent); // POC: Commands are tightly coupled with their bindings, at least for now, saves us injecting a factory.
    SubscribeToRhinoEvents();
  }

  private void SubscribeToRhinoEvents()
  {
    // POC: It is unclear to me why is the binding keeping track of ChangedObjectIds. Change tracking should be moved to a separate type.
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

  public List<ISendFilter> GetSendFilters() => _sendFilters;

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
  public async Task Send(string modelCardId)
  {
    using var unitOfWork = _unitOfWorkFactory.Resolve<SendOperation<RhinoObject>>();
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      List<RhinoObject> rhinoObjects = modelCard.SendFilter
        .GetObjectIds()
        .Select(id => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id)))
        .Where(obj => obj != null)
        .ToList();

      var sendInfo = new SendInfo()
      {
        AccountId = modelCard.AccountId,
        ProjectId = modelCard.ProjectId,
        ModelId = modelCard.ModelId,
        ConvertedObjects = _convertedObjectReferences,
        ChangedObjectIds = modelCard.ChangedObjectIds,
        SourceApplication = _rhinoSettings.HostAppInfo.Name
      };

      var sendResult = await _sendOperation
        .Execute(
          rhinoObjects,
          sendInfo,
          (status, progress) => OnSendOperationProgress(modelCardId, status, progress),
          cts.Token
        )
        .ConfigureAwait(false);

      // Store the converted references in memory for future send operations, overwriting the existing values for the given application id.
      foreach (var kvp in sendResult.convertedReferences)
      {
        _convertedObjectReferences[kvp.Key + modelCard.ProjectId] = kvp.Value;
      }

      // It's important to reset the model card's list of changed obj ids so as to ensure we accurately keep track of changes between send operations.
      modelCard.ChangedObjectIds = new();

      Commands.SetModelCreatedVersionId(modelCardId, sendResult.rootObjId);
    }
    catch (OperationCanceledException)
    {
      return;
    }
    catch (Exception e) when (!e.IsFatal()) // All exceptions should be handled here if possible, otherwise we enter "crashing the host app" territory.
    {
      Commands.SetModelError(modelCardId, e);
    }
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress { Status = status, Progress = progress });
  }

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  /// <summary>
  /// Checks if any sender model cards contain any of the changed objects. If so, also updates the changed objects hashset for each model card - this last part is important for on send change detection.
  /// </summary>
  private void RunExpirationChecks()
  {
    List<SenderModelCard> senders = _store.GetSenders();
    string[] objectIdsList = ChangedObjectIds.ToArray();
    List<string> expiredSenderIds = new();

    foreach (SenderModelCard modelCard in senders)
    {
      var intersection = modelCard.SendFilter.GetObjectIds().Intersect(objectIdsList).ToList();
      bool isExpired = modelCard.SendFilter.CheckExpiry(ChangedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(modelCard.ModelCardId);
        modelCard.ChangedObjectIds.UnionWith(intersection);
      }
    }

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  public void Dispose()
  {
    IsDisposed = true;
  }

  public bool IsDisposed { get; private set; }
}
