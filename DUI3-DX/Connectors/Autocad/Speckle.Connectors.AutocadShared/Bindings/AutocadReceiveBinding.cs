using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Autocad.Bindings;

public sealed class AutocadReceiveBinding : IReceiveBinding
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
        // TODO: this will crash autocad
        throw new InvalidOperationException("No download model card was found.");
      }

      // Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // Document activation event handler to cancel operation if document is switched
      Autodesk.AutoCAD.ApplicationServices.DocumentCollectionEventHandler? documentActivatedDuringOperation = null;
      documentActivatedDuringOperation = (_, _) =>
      {
        Application.DocumentManager.DocumentActivated -= documentActivatedDuringOperation;
        CancelReceive(modelCardId);
        Commands.SetGlobalNotification(
          ToastNotificationType.WARNING,
          "Load cancelled",
          "Load operation in progress was cancelled due to document activation."
        );
      };

      Application.DocumentManager.DocumentActivated += documentActivatedDuringOperation;

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
}
