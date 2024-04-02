using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Core.Logging;
using Speckle.Connectors.Utils;
using System.Threading.Tasks;
using System.Threading;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Connectors.Revit.Operations.Send;
using Speckle.Connectors.DUI.Models.Card;

namespace Speckle.Connectors.Revit.Bindings;

internal class SendBinding : RevitBaseBinding, ICancelable, ISendBinding
{
  // POC:does it need injecting?
  public CancellationManager CancellationManager { get; } = new();

  // POC: does it need injecting?
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  // In the context of the SEND operation, we're only ever expecting ONE conversion
  private readonly SendOperation _sendOperation;
  private readonly IRevitIdleManager _idleManager;

  public SendBinding(
    IRevitIdleManager idleManager,
    RevitContext revitContext,
    RevitDocumentStore store,
    IBridge bridge,
    SendOperation sendOperation
  )
    : base("sendBinding", store, bridge, revitContext)
  {
    _idleManager = idleManager;
    Commands = new SendBindingUICommands(bridge);
    _sendOperation = sendOperation;

    // TODO expiry events
    // TODO filters need refresh events
    revitContext.UIApplication.Application.DocumentChanged += (_, e) => DocChangeHandler(e);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new RevitEverythingFilter(), new RevitSelectionFilter() };
  }

  public async Task Send(string modelCardId)
  {
    await SpeckleTopLevelExceptionHandler
      .Run(() => HandleSend(modelCardId), HandleSpeckleException, HandleUnexpectedException, HandleFatalException)
      .ConfigureAwait(false);
  }

  public void CancelSend(string modelCardId)
  {
    CancellationManager.CancelOperation(modelCardId);
  }

  public SendBindingUICommands Commands { get; }

  private async Task HandleSend(string modelCardId)
  {
    CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

    if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
    {
      throw new InvalidOperationException("No publish model card was found.");
    }

    string versionId = await _sendOperation
      .Execute(
        modelCard.SendFilter,
        modelCard.AccountId,
        modelCard.ProjectId,
        modelCard.ModelId,
        (status, progress) => OnSendOperationProgress(modelCardId, status, progress),
        cts.Token
      )
      .ConfigureAwait(false);

    Commands.SetModelCreatedVersionId(modelCardId, versionId);
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress { Status = status, Progress = progress });
  }

  private bool HandleSpeckleException(SpeckleException spex)
  {
    // POC: do something here

    return false;
  }

  private bool HandleUnexpectedException(Exception ex)
  {
    // POC: do something here

    return false;
  }

  private bool HandleFatalException(Exception ex)
  {
    // POC: do something here

    return false;
  }

  /// <summary>
  /// Keeps track of the changed element ids as well as checks if any of them need to trigger
  /// a filter refresh (e.g., views being added).
  /// </summary>
  /// <param name="e"></param>
  private void DocChangeHandler(Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
  {
    ICollection<ElementId> addedElementIds = e.GetAddedElementIds();
    ICollection<ElementId> deletedElementIds = e.GetDeletedElementIds();
    ICollection<ElementId> modifiedElementIds = e.GetModifiedElementIds();

    foreach (ElementId elementId in addedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in deletedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in modifiedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    // TODO: CHECK IF ANY OF THE ABOVE ELEMENTS NEED TO TRIGGER A FILTER REFRESH
    _idleManager.SubscribeToIdle(RunExpirationChecks);
  }

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
}
