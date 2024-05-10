using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Connectors.Utils.Operations;

/// <summary>
/// Default implementation of the <see cref="IRootObjectSender"/> which takes a <see cref="Base"/> and sends
/// it to a server described by the parameters in the <see cref="Send"/> method
/// </summary>
/// POC: we have a generic RootObjectSender but we're not using it everywhere. It also appears to need some specialisation or at least
/// a way to get the application name, so RevitContext is being used in the revit version but we could probably inject that as a IHostAppContext maybe?
public sealed class RootObjectSender : IRootObjectSender
{
  // POC: Revisit this factory pattern, I think we could solve this higher up by injecting a scoped factory for `SendOperation` in the SendBinding
  private readonly ServerTransport.Factory _transportFactory;

  public RootObjectSender(ServerTransport.Factory transportFactory)
  {
    _transportFactory = transportFactory;
  }

  public async Task<(string rootObjId, Dictionary<string, ObjectReference> convertedReferences)> Send(
    Base commitObject,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    ct.ThrowIfCancellationRequested();

    onOperationProgressed?.Invoke("Uploading...", null);

    Account account = AccountManager.GetAccount(sendInfo.AccountId);

    ITransport transport = _transportFactory(account, sendInfo.ProjectId, 60, null);
    var sendResult = await SendHelper.Send(commitObject, transport, true, null, ct).ConfigureAwait(false);

    ct.ThrowIfCancellationRequested();

    onOperationProgressed?.Invoke("Linking version to model...", null);

    // 8 - Create the version (commit)
    using var apiClient = new Client(account);
    _ = await apiClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = sendInfo.ProjectId,
          branchName = sendInfo.ModelId,
          sourceApplication = sendInfo.SourceApplication,
          objectId = sendResult.rootObjId
        },
        ct
      )
      .ConfigureAwait(true);

    return sendResult;
  }
}
