using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Converters.Common;
using Speckle.Core.Transports;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Api;
using Speckle.Core.Models;
using System.Diagnostics;
using ICancelable = System.Reactive.Disposables.ICancelable;
using Speckle.Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Speckle.Connectors.Autocad.Bindings;

public sealed class AutocadReceiveBinding : IReceiveBinding, ICancelable
{
  public string Name { get; } = "receiveBinding";
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly CancellationManager _cancellationManager;

  //private readonly IScopedFactory<ISpeckleConverterToSpeckle> _speckleConverterToSpeckleFactory;

  public AutocadReceiveBinding(DocumentModelStore store, IBridge parent, CancellationManager cancellationManager)
  {
    _store = store;
    //_speckleConverterToSpeckleFactory = speckleConverterToSpeckleFactory;
    _cancellationManager = cancellationManager;

    Parent = parent;
  }

  public void CancelReceive(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public Task Receive(string modelCardId, string versionId)
  {
    Parent.RunOnMainThread(async () => await ReceiveInternal(modelCardId, versionId).ConfigureAwait(false));
    return Task.CompletedTask;
  }

  private async Task ReceiveInternal(string modelCardId, string versionId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get receiver card
      if (_store.GetModelById(modelCardId) is not ReceiverModelCard modelCard)
      {
        throw new InvalidOperationException("No download model card was found.");
      }

      // 2 - Check account exist
      Account account =
        AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == modelCard.AccountId)
        ?? throw new SpeckleAccountManagerException();

      // 3 - Get commit object from server
      Client apiClient = new(account);
      ServerTransport transport = new(account, modelCard.ProjectId);
      Commit? version =
        await apiClient.CommitGet(modelCard.ProjectId, versionId, cts.Token).ConfigureAwait(false)
        ?? throw new SpeckleException($"Failed to receive commit: {versionId} from server)");

      Base? commitObject =
        await Operations
          .Receive(version.id, cancellationToken: cts.Token, remoteTransport: transport)
          .ConfigureAwait(false)
        ?? throw new SpeckleException(
          $"Failed to receive commit: {version.id} objects from server: {nameof(Operations)} returned null"
        );

      apiClient.Dispose();
      cts.Token.ThrowIfCancellationRequested();

      // 4 - Convert objects
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

  private Base ConvertObjects(
    List<(DBObject obj, string applicationId)> dbObjects,
    ReceiverModelCard modelCard,
    CancellationToken cancellationToken
  )
  {
    //ISpeckleConverterToNative converter = _speckleConverterToNativeFactory.ResolveScopedInstance();
  }

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public void Dispose()
  {
    IsDisposed = true;
    //_speckleConverterToNativeFactory.Dispose();
  }

  public bool IsDisposed { get; private set; }

  private static readonly string[] s_separator = new[] { "\\" };
}
