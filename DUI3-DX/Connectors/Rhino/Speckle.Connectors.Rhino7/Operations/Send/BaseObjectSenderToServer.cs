using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Connectors.Rhino7.Operations.Send;

/// <summary>
/// Default implementation of the <see cref="IBaseObjectSender"/> which takes a <see cref="Base"/> and sends
/// it to a server described by the parameters in the <see cref="Send"/> method
/// </summary>
internal sealed class BaseObjectSenderToServer : IBaseObjectSender
{
  private readonly Func<Account, string, ITransport> _transportFactory;

  public BaseObjectSenderToServer(Func<Account, string, ITransport> transportFactory)
  {
    _transportFactory = transportFactory;
  }

  public async Task<string> Send(
    Base commitObject,
    string accountId,
    string projectId,
    string modelId,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    ct.ThrowIfCancellationRequested();

    onOperationProgressed?.Invoke("Uploading...", null);

    // TODO: FETCHING ACCOUNTS BY ID ONLY IS UNSAFE
    Account account =
      AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == accountId)
      ?? throw new SpeckleAccountManagerException();

    ITransport transport = _transportFactory(account, projectId);
    var sendResult = await SendHelper.Send(commitObject, transport, true, null, ct).ConfigureAwait(true);

    ct.ThrowIfCancellationRequested();
    //// Store the converted references in memory for future send operations, overwriting the existing values for the given application id.
    //foreach (var kvp in sendResult.ConvertedReferences)
    //{
    //  // TODO: Bug in here, we need to encapsulate cache not only by app id, but also by project id,
    //  // TODO: as otherwise we assume incorrectly that an object exists for a given project (e.g, send box to project 1, send same unchanged box to project 2)
    //  _convertedObjectReferences[kvp.Key + projectId] = kvp.Value;
    //}
    // It's important to reset the model card's list of changed obj ids so as to ensure we accurately keep track of changes between send operations.
    // NOTE: ChangedObjectIds is currently JsonIgnored, but could actually be useful for highlighting changes in host app.
    //modelCard.ChangedObjectIds = new();

    onOperationProgressed?.Invoke("Linking version to model...", null);

    // 8 - Create the version (commit)
    using var apiClient = new Client(account);
    string versionId = await apiClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = projectId,
          branchName = modelId,
          sourceApplication = "Rhino",
          objectId = sendResult.rootObjId
        },
        ct
      )
      .ConfigureAwait(true);

    return versionId;
  }
}
