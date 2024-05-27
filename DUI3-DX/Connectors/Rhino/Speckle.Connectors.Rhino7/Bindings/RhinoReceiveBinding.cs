using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Logging;
using NotNullExtensions = Speckle.Connectors.Utils.NotNullExtensions;

namespace Speckle.Connectors.Rhino7.Bindings;

public class RhinoReceiveBinding : IReceiveBinding, ICancelable
{
  public string Name { get; set; } = "receiveBinding";
  public IBridge Parent { get; set; }
  public CancellationManager CancellationManager { get; set; }

  private readonly DocumentModelStore _store;
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  public ReceiveBindingUICommands Commands { get; }

  public RhinoReceiveBinding(
    DocumentModelStore store,
    CancellationManager cancellationManager,
    IBridge parent,
    IUnitOfWorkFactory unitOfWorkFactory
  )
  {
    Parent = parent;
    _store = store;
    _unitOfWorkFactory = unitOfWorkFactory;
    CancellationManager = cancellationManager;
    Commands = new ReceiveBindingUICommands(parent);
  }

  public void CancelReceive(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  public async Task Receive(string modelCardId)
  {
    using var unitOfWork = _unitOfWorkFactory.Resolve<ReceiveOperation>();
    try
    {
      // Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // Get receiver card
      if (_store.GetModelById(modelCardId) is not ReceiverModelCard modelCard)
      {
        throw new InvalidOperationException("No download model card was found.");
      }

      // Receive host objects
      IReadOnlyList<ReceiveConversionResult> conversionResults = await unitOfWork.Service
        .Execute(
          NotNullExtensions.NotNull(modelCard.AccountId), // POC: I hear -you are saying why we're passing them separately. Not sure pass the DUI3-> Connectors.DUI project dependency to the SDK-> Connector.Utils
          NotNullExtensions.NotNull(modelCard.ProjectId),
          NotNullExtensions.NotNull(modelCard.ProjectName),
          NotNullExtensions.NotNull(modelCard.ModelName),
          NotNullExtensions.NotNull(modelCard.SelectedVersionId),
          cts.Token,
          (status, progress) => OnSendOperationProgress(modelCardId, status, progress)
        )
        .ConfigureAwait(false);

      Commands.SetModelReceiveResult(modelCardId, conversionResults);
    }
    catch (Exception e) when (!e.IsFatal()) // All exceptions should be handled here if possible, otherwise we enter "crashing the host app" territory.
    {
      Commands.SetModelError(modelCardId, e);
    }
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress(modelCardId, status, progress));
  }

  public void CancelSend(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  public void Dispose()
  {
    IsDisposed = true;
  }

  public bool IsDisposed { get; private set; }
}
