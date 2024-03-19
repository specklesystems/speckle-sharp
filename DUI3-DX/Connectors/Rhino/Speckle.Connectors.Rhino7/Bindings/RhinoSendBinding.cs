using System;
using System.Collections.Generic;
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
using Speckle.Connectors.Rhino7.Operations.Send;

namespace Speckle.Connectors.Rhino7.Bindings;

public sealed class RhinoSendBinding : ISendBinding, ICancelable
{
  public string Name { get; } = "sendBinding";
  public SendBindingUICommands Commands { get; }
  public IBridge Parent { get; set; }

  private readonly DocumentModelStore _store;
  private readonly RhinoIdleManager _idleManager;

  private readonly IBasicConnectorBinding _basicConnectorBinding;
  private readonly List<ISendFilter> _sendFilters;
  private readonly Func<SendOperation> _sendOperationFactory;
  private readonly CancellationManager _cancellationManager;

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  /// <summary>
  /// Keeps track of previously converted objects as a dictionary of (applicationId, object reference).
  /// </summary>
  //private readonly Dictionary<string, ObjectReference> _convertedObjectReferences = new();

  public RhinoSendBinding(
    DocumentModelStore store,
    RhinoIdleManager idleManager,
    IBridge parent,
    IBasicConnectorBinding basicConnectorBinding,
    IEnumerable<ISendFilter> sendFilters,
    Func<SendOperation> sendOperationFactory,
    CancellationManager cancellationManager
  )
  {
    _store = store;
    _idleManager = idleManager;
    _basicConnectorBinding = basicConnectorBinding;
    _sendFilters = sendFilters.ToList();
    _sendOperationFactory = sendOperationFactory;
    _cancellationManager = cancellationManager;
    Parent = parent;

    // would like to know more about binding, parent relationship
    // before injecting this
    Commands = new SendBindingUICommands(parent);
    SubscriptToRhinoEvents();
  }

  private void SubscriptToRhinoEvents()
  {
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

  public async Task Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      await _sendOperationFactory()
        .Execute(
          modelCard.SendFilter,
          modelCard.AccountId,
          modelCard.ProjectId,
          modelCard.ModelId,
          (status, progress) => OnSendOperationProgress(modelCardId, status, progress),
          (versionId) => Commands.SetModelCreatedVersionId(modelCardId, versionId),
          cts.Token
        )
        .ConfigureAwait(false);
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

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    _basicConnectorBinding.Commands.SetModelProgress(
      modelCardId,
      new ModelCardProgress { Status = status, Progress = progress }
    );
  }

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

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
    // TODO release managed resources here
    IsDisposed = true;
  }

  public bool IsDisposed { get; private set; }
}
