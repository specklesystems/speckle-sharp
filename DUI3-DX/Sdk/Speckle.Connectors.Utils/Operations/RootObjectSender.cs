using System.Diagnostics;
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
  // private readonly ServerV3.Factory _transportFactory;

  public RootObjectSender()
  {
    // _transportFactory = transportFactory;
  }

  public async Task<(string rootObjId, Dictionary<string, ObjectReference> convertedReferences)> Send(
    Base commitObject,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    Account account = AccountManager.GetAccount(sendInfo.AccountId);

    ITransport v2 = new ServerTransport(account, sendInfo.ProjectId);
    await SendInternal(v2, commitObject, sendInfo, "V2", onOperationProgressed, ct).ConfigureAwait(false);

    ITransport v3 = new ServerTransport(account, sendInfo.ProjectId, useNewPipes: true);
    await SendInternal(v3, commitObject, sendInfo, "V3", onOperationProgressed, ct).ConfigureAwait(false);

    ITransport v4 = new ServerV4(account, sendInfo.ProjectId, ct);
    return await SendInternal(v4, commitObject, sendInfo, "V4", onOperationProgressed, ct).ConfigureAwait(false);
  }

  public async Task<(string rootObjId, Dictionary<string, ObjectReference> convertedReferences)> SendInternal(
    ITransport tr,
    Base commitObject,
    SendInfo sendInfo,
    string version,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    ct.ThrowIfCancellationRequested();

    onOperationProgressed?.Invoke("Uploading...", null);

    Account account = AccountManager.GetAccount(sendInfo.AccountId);
    var sendTimer = Stopwatch.StartNew();

    var sendResult = await SendHelper.Send(commitObject, tr, true, null, ct).ConfigureAwait(false);

    Debug.WriteLine(
      $"{version}: Finished sending {tr.SavedObjectCount} objects after {sendTimer.Elapsed.TotalSeconds}s."
    );

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
