using Autodesk.Revit.DB;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Utils;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Exceptions;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Revit.Bindings;

internal sealed class RevitSendBinding : RevitBaseBinding, ISendBinding
{
  // POC:does it need injecting?

  // POC: does it need injecting?
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  private readonly RevitSettings _revitSettings;
  private readonly IRevitIdleManager _idleManager;
  private readonly CancellationManager _cancellationManager;
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  private readonly ISendConversionCache _sendConversionCache;
  private readonly ITopLevelExceptionHandler _topLevelExceptionHandler;

  public RevitSendBinding(
    IRevitIdleManager idleManager,
    RevitContext revitContext,
    DocumentModelStore store,
    CancellationManager cancellationManager,
    IBridge bridge,
    IUnitOfWorkFactory unitOfWorkFactory,
    RevitSettings revitSettings,
    ISendConversionCache sendConversionCache,
    ITopLevelExceptionHandler topLevelExceptionHandler
  )
    : base("sendBinding", store, bridge, revitContext)
  {
    _idleManager = idleManager;
    _cancellationManager = cancellationManager;
    _unitOfWorkFactory = unitOfWorkFactory;
    _revitSettings = revitSettings;
    _sendConversionCache = sendConversionCache;
    _topLevelExceptionHandler = topLevelExceptionHandler;

    Commands = new SendBindingUICommands(bridge);
    // TODO expiry events
    // TODO filters need refresh events
    revitContext.UIApplication.NotNull().Application.DocumentChanged += (_, e) =>
      _topLevelExceptionHandler.CatchUnhandled(() => DocChangeHandler(e));

    Store.DocumentChanged += (_, _) => _topLevelExceptionHandler.CatchUnhandled(OnDocumentChanged);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new RevitSelectionFilter() { IsDefault = true } };
  }

  public void CancelSend(string modelCardId)
  {
    _cancellationManager.CancelOperation(modelCardId);
  }

  public SendBindingUICommands Commands { get; }

  public async Task Send(string modelCardId)
  {
    // Note: removed top level handling thing as it was confusing me
    try
    {
      if (Store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        // Handle as GLOBAL ERROR at BrowserBridge
        throw new InvalidOperationException("No publish model card was found.");
      }

      // POC: probably the CTS SHOULD be injected as InstancePerLifetimeScope and then
      // it can be injected where needed instead of passing it around like a bomb :D
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

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
        _revitSettings.HostSlug.NotNull()
      );

      var sendResult = await sendOperation.Service
        .Execute(
          revitObjects,
          sendInfo,
          (status, progress) =>
            Commands.SetModelProgress(modelCardId, new ModelCardProgress(modelCardId, status, progress), cts),
          cts.Token
        )
        .ConfigureAwait(false);

      Commands.SetModelSendResult(modelCardId, sendResult.RootObjId, sendResult.ConversionResults);
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

    _sendConversionCache.EvictObjects(objectIdsList);

    foreach (SenderModelCard modelCard in senders)
    {
      var intersection = modelCard.SendFilter.NotNull().GetObjectIds().Intersect(objectIdsList).ToList();
      bool isExpired = intersection.Count != 0;
      if (isExpired)
      {
        expiredSenderIds.Add(modelCard.ModelCardId.NotNull());
      }
    }

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  // POC: Will be re-addressed later with better UX with host apps that are friendly on async doc operations.
  // That's why don't bother for now how to get rid of from dup logic in other bindings.
  private void OnDocumentChanged()
  {
    if (_cancellationManager.NumberOfOperations > 0)
    {
      _cancellationManager.CancelAllOperations();
      Commands.SetGlobalNotification(
        ToastNotificationType.INFO,
        "Document Switch",
        "Operations cancelled because of document swap!"
      );
    }
  }
}
