using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Logging;
using ICancelable = System.Reactive.Disposables.ICancelable;

namespace Speckle.Connectors.Autocad.Bindings;

public sealed class AutocadReceiveBinding : IReceiveBinding, ICancelable
{
  public string Name { get; } = "receiveBinding";
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly CancellationManager _cancellationManager;

  public ReceiveBindingUICommands Commands { get; }

  private readonly ReceiveOperation _receiveOperation;

  public AutocadReceiveBinding(
    DocumentModelStore store,
    IBridge parent,
    ReceiveOperation receiveOperation,
    CancellationManager cancellationManager
  )
  {
    _store = store;
    _cancellationManager = cancellationManager;
    _receiveOperation = receiveOperation;
    Parent = parent;
    Commands = new ReceiveBindingUICommands(parent);
  }

  public void CancelReceive(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public Task Receive(string modelCardId)
  {
    Parent.RunOnMainThread(async () => await ReceiveInternal(modelCardId).ConfigureAwait(false));
    return Task.CompletedTask;
  }

  private async Task ReceiveInternal(string modelCardId)
  {
    try
    {
      // Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // Get receiver card
      if (_store.GetModelById(modelCardId) is not ReceiverModelCard modelCard)
      {
        throw new InvalidOperationException("No download model card was found.");
      }

      // Receive host objects
      IEnumerable<string> receivedObjectIds = await _receiveOperation
        .Execute(
          modelCard.AccountId, // POC: I hear -you are saying why we're passing them separately. Not sure pass the DUI3-> Connectors.DUI project dependency to the SDK-> Connector.Utils
          modelCard.ProjectId,
          modelCard.ProjectName,
          modelCard.ModelName,
          modelCard.SelectedVersionId,
          cts,
          (status, progress) => OnSendOperationProgress(modelCardId, status, progress)
        )
        .ConfigureAwait(false);

      Commands.SetModelReceiveResult(modelCardId, receivedObjectIds.ToList());
    }
    catch (OperationCanceledException)
    {
      // POC: not sure here need to handle anything. UI already aware it cancelled operation visually.
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

  public void Dispose()
  {
    IsDisposed = true;
  }

  public bool IsDisposed { get; private set; }
}
