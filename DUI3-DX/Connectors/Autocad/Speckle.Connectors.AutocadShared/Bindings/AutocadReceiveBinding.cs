using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Operations;
using ICancelable = System.Reactive.Disposables.ICancelable;

namespace Speckle.Connectors.Autocad.Bindings;

public sealed class AutocadReceiveBinding : IReceiveBinding, ICancelable
{
  public string Name => "receiveBinding";
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly CancellationManager _cancellationManager;
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;

  public ReceiveBindingUICommands Commands { get; }

  public AutocadReceiveBinding(
    DocumentModelStore store,
    IBridge parent,
    CancellationManager cancellationManager,
    IUnitOfWorkFactory unitOfWorkFactory
  )
  {
    _store = store;
    _cancellationManager = cancellationManager;
    _unitOfWorkFactory = unitOfWorkFactory;
    Parent = parent;
    Commands = new ReceiveBindingUICommands(parent);
    _store.DocumentChanged += (_, _) => OnDocumentChanged();
  }

  public void CancelReceive(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public async Task Receive(string modelCardId)
  {
    using var unitOfWork = _unitOfWorkFactory.Resolve<ReceiveOperation>();
    try
    {
      // Get receiver card
      if (_store.GetModelById(modelCardId) is not ReceiverModelCard modelCard)
      {
        // Handle as GLOBAL ERROR at BrowserBridge
        throw new InvalidOperationException("No download model card was found.");
      }

      // Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // Receive host objects
      var operationResults = await unitOfWork.Service
        .Execute(
          modelCard.AccountId.NotNull(), // POC: I hear -you are saying why we're passing them separately. Not sure pass the DUI3-> Connectors.DUI project dependency to the SDK-> Connector.Utils
          modelCard.ProjectId.NotNull(),
          modelCard.ProjectName.NotNull(),
          modelCard.ModelName.NotNull(),
          modelCard.SelectedVersionId.NotNull(),
          cts.Token,
          onOperationProgressed: (status, progress) => OnSendOperationProgress(modelCardId, status, progress)
        )
        .ConfigureAwait(false);

      Commands.SetModelReceiveResult(modelCardId, operationResults.BakedObjectIds, operationResults.ConversionResults);
    }
    // Catch here specific exceptions if they related to model card.
    catch (OperationCanceledException)
    {
      // SWALLOW -> UI handles it immediately, so we do not need to handle anything
      return;
    }
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress(modelCardId, status, progress));
  }

  private void OnDocumentChanged()
  {
    if (_cancellationManager.NumberOfOperations > 0)
    {
      _cancellationManager.CancelAllOperations();
      // POC: Will be readdressed later with better UX with host apps which are friendly on async doc operations.
      Commands.SendGlobalNotification(
        ToastNotificationType.INFO,
        "Document Switch",
        "Operations cancelled because of document swap!"
      );
    }
  }

  public void Dispose()
  {
    IsDisposed = true;
  }

  public bool IsDisposed { get; private set; }
}
