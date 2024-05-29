using Autodesk.Revit.DB;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Core.Logging;
using Speckle.Connectors.Utils;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Exceptions;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models;

namespace Speckle.Connectors.Revit.Bindings;

internal sealed class SendBinding : RevitBaseBinding, ICancelable, ISendBinding
{
  // POC:does it need injecting?
  public CancellationManager CancellationManager { get; } = new();

  // POC: does it need injecting?
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  /// <summary>
  /// Keeps track of previously converted objects as a dictionary of (applicationId, object reference).
  /// </summary>
  private readonly Dictionary<string, ObjectReference> _convertedObjectReferences = new();

  private readonly RevitSettings _revitSettings;
  private readonly IRevitIdleManager _idleManager;
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;

  public SendBinding(
    IRevitIdleManager idleManager,
    RevitContext revitContext,
    DocumentModelStore store,
    IBridge bridge,
    IUnitOfWorkFactory unitOfWorkFactory,
    RevitSettings revitSettings
  )
    : base("sendBinding", store, bridge, revitContext)
  {
    _idleManager = idleManager;
    _unitOfWorkFactory = unitOfWorkFactory;
    _revitSettings = revitSettings;
    Commands = new SendBindingUICommands(bridge);
    // TODO expiry events
    // TODO filters need refresh events
    revitContext.UIApplication.NotNull().Application.DocumentChanged += (_, e) => DocChangeHandler(e);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new RevitSelectionFilter() { IsDefault = true } };
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
    // POC: this try catch pattern is coming from Rhino. I see there is a semi implemented "SpeckleTopLevelExceptionHandler"
    // above that wraps the call of this HandleSend, but it currently is not doing anything - resulting in all errors from here
    // bubbling up to the bridge.
    try
    {
      if (Store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        // Handle as GLOBAL ERROR at BrowserBridge
        throw new InvalidOperationException("No publish model card was found.");
      }

      // POC: probably the CTS SHOULD be injected as InstancePerLifetimeScope and then
      // it can be injected where needed instead of passing it around like a bomb :D
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      using IUnitOfWork<SendOperation<ElementId>> sendOperation = _unitOfWorkFactory.Resolve<
        SendOperation<ElementId>
      >();

      List<ElementId> revitObjects = modelCard.SendFilter
        .NotNull()
        .GetObjectIds()
        .Select(id => ElementId.Parse(id))
        .ToList();

      if (revitObjects.Count == 0)
      {
        // Handle as CARD ERROR in this function
        throw new SpeckleSendFilterException("No objects were found to convert. Please update your publish filter!");
      }

      var sendInfo = new SendInfo(
        modelCard.AccountId.NotNull(),
        modelCard.ProjectId.NotNull(),
        modelCard.ModelId.NotNull(),
        _revitSettings.HostSlug.NotNull(),
        _convertedObjectReferences,
        modelCard.ChangedObjectIds
      );

      var sendResult = await sendOperation.Service
        .Execute(
          revitObjects,
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
    // Catch here specific exceptions if they related to model card.
    catch (SpeckleSendFilterException e)
    {
      Commands.SetModelError(modelCardId, e);
    }
    catch (OperationCanceledException)
    {
      return;
    }
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress(modelCardId, status, progress));
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
      ChangedObjectIds.Add(elementId.ToString());
    }

    foreach (ElementId elementId in deletedElementIds)
    {
      ChangedObjectIds.Add(elementId.ToString());
    }

    foreach (ElementId elementId in modifiedElementIds)
    {
      ChangedObjectIds.Add(elementId.ToString());
    }

    // TODO: CHECK IF ANY OF THE ABOVE ELEMENTS NEED TO TRIGGER A FILTER REFRESH
    _idleManager.SubscribeToIdle(RunExpirationChecks);
  }

  private void RunExpirationChecks()
  {
    var senders = Store.GetSenders();
    string[] objectIdsList = ChangedObjectIds.ToArray();
    List<string> expiredSenderIds = new();

    foreach (SenderModelCard modelCard in senders)
    {
      var intersection = modelCard.SendFilter.NotNull().GetObjectIds().Intersect(objectIdsList).ToList();
      bool isExpired = modelCard.SendFilter.CheckExpiry(ChangedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(modelCard.ModelCardId.NotNull());
        modelCard.ChangedObjectIds.UnionWith(intersection);
      }
    }

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }
}
