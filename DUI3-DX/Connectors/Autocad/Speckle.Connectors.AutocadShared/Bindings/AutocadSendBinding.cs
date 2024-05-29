using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.Operations.Send;
using Speckle.Connectors.DUI.Exceptions;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models;
using ICancelable = System.Reactive.Disposables.ICancelable;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils;

namespace Speckle.Connectors.Autocad.Bindings;

public sealed class AutocadSendBinding : ISendBinding, ICancelable
{
  public string Name { get; } = "sendBinding";
  public SendBindingUICommands Commands { get; }
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly AutocadIdleManager _idleManager;
  private readonly List<ISendFilter> _sendFilters;
  private readonly CancellationManager _cancellationManager;
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  private readonly AutocadSettings _autocadSettings;

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  /// <summary>
  /// Keeps track of previously converted objects as a dictionary of (applicationId, object reference).
  /// </summary>
  private readonly Dictionary<string, ObjectReference> _convertedObjectReferences = new();

  public AutocadSendBinding(
    DocumentModelStore store,
    AutocadIdleManager idleManager,
    IBridge parent,
    IEnumerable<ISendFilter> sendFilters,
    CancellationManager cancellationManager,
    AutocadSettings autocadSettings,
    IUnitOfWorkFactory unitOfWorkFactory
  )
  {
    _store = store;
    _idleManager = idleManager;
    _unitOfWorkFactory = unitOfWorkFactory;
    _autocadSettings = autocadSettings;
    _cancellationManager = cancellationManager;
    _sendFilters = sendFilters.ToList();

    Parent = parent;
    Commands = new SendBindingUICommands(parent);

    Application.DocumentManager.DocumentActivated += (sender, args) => SubscribeToObjectChanges(args.Document);
    if (Application.DocumentManager.CurrentDocument != null)
    {
      // catches the case when autocad just opens up with a blank new doc
      SubscribeToObjectChanges(Application.DocumentManager.CurrentDocument);
    }
  }

  private readonly List<string> _docSubsTracker = new();

  private void SubscribeToObjectChanges(Document doc)
  {
    if (doc == null || doc.Database == null || _docSubsTracker.Contains(doc.Name))
    {
      return;
    }

    _docSubsTracker.Add(doc.Name);
    doc.Database.ObjectAppended += (_, e) => OnChangeChangedObjectIds(e.DBObject);
    doc.Database.ObjectErased += (_, e) => OnChangeChangedObjectIds(e.DBObject);
    doc.Database.ObjectModified += (_, e) => OnChangeChangedObjectIds(e.DBObject);
  }

  private void OnChangeChangedObjectIds(DBObject dBObject)
  {
    ChangedObjectIds.Add(dBObject.Handle.Value.ToString());
    _idleManager.SubscribeToIdle(RunExpirationChecks);
  }

  private void RunExpirationChecks()
  {
    var senders = _store.GetSenders();
    string[] objectIdsList = ChangedObjectIds.ToArray();
    List<string> expiredSenderIds = new();

    foreach (SenderModelCard modelCard in senders)
    {
      var intersection = modelCard.SendFilter.NotNull().GetObjectIds().Intersect(objectIdsList).ToList();
      bool isExpired = intersection.Count != 0;
      if (isExpired)
      {
        expiredSenderIds.Add(modelCard.ModelCardId.NotNull());
        modelCard.ChangedObjectIds.UnionWith(intersection);
      }
    }

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  public List<ISendFilter> GetSendFilters() => _sendFilters;

  public Task Send(string modelCardId)
  {
    Parent.RunOnMainThread(async () => await SendInternal(modelCardId).ConfigureAwait(false));
    return Task.CompletedTask;
  }

  private async Task SendInternal(string modelCardId)
  {
    try
    {
      if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        // Handle as GLOBAL ERROR at BrowserBridge
        throw new InvalidOperationException("No publish model card was found.");
      }

      using var uow = _unitOfWorkFactory.Resolve<SendOperation<AutocadRootObject>>();

      // Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // Get elements to convert
      List<AutocadRootObject> autocadObjects = Application.DocumentManager.CurrentDocument.GetObjects(
        modelCard.SendFilter.NotNull().GetObjectIds()
      );

      if (autocadObjects.Count == 0)
      {
        // Handle as CARD ERROR in this function
        throw new SpeckleSendFilterException("No objects were found to convert. Please update your publish filter!");
      }

      var sendInfo = new SendInfo(
        modelCard.AccountId.NotNull(),
        modelCard.ProjectId.NotNull(),
        modelCard.ModelId.NotNull(),
        _autocadSettings.HostAppInfo.Name,
        _convertedObjectReferences,
        modelCard.ChangedObjectIds
      );

      var sendResult = await uow.Service
        .Execute(
          autocadObjects,
          sendInfo,
          (status, progress) => OnSendOperationProgress(modelCardId, status, progress),
          cts.Token
        )
        .ConfigureAwait(false);

      // Store the converted references in memory for future send operations, overwriting the existing values for the given application id.
      foreach (var kvp in sendResult.ConvertedReferences)
      {
        _convertedObjectReferences[kvp.Key + modelCard.ProjectId] = kvp.Value;
      }

      // It's important to reset the model card's list of changed obj ids so as to ensure we accurately keep track of changes between send operations.
      modelCard.ChangedObjectIds = new();

      Commands.SetModelCreatedVersionId(modelCardId, sendResult.RootObjId, sendResult.ConversionResults.Results);
    }
    // Catch here specific exceptions if they related to model card.
    catch (OperationCanceledException)
    {
      // SWALLOW -> UI handles it immediately, so we do not need to handle anything
      return;
    }
    catch (SpeckleSendFilterException e)
    {
      Commands.SetModelError(modelCardId, e);
    }
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress(modelCardId, status, progress));
  }

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public void Dispose()
  {
    IsDisposed = true;
  }

  public bool IsDisposed { get; private set; }
}
