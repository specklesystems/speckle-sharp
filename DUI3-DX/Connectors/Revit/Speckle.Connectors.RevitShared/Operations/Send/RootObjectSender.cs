using System;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Api;
using System.Threading.Tasks;
using System.Threading;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Connectors.Revit.Operations.Send;

/// <summary>
/// Default implementation of the <see cref="IRootObjectSender"/> which takes a <see cref="Base"/> and sends
/// it to a server described by the parameters in the <see cref="Send"/> method
/// </summary>
internal class RootObjectSender : IRootObjectSender
{
  // POC: unsure about this factory pattern - a little weakly typed (being a Func)
  private readonly ServerTransport.Factory _transportFactory;
  private readonly RevitSettings _revitSettings;

  public RootObjectSender(ServerTransport.Factory transportFactory, RevitSettings revitSettings)
  {
    _transportFactory = transportFactory;
    _revitSettings = revitSettings;
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

    Account account = AccountManager.GetAccount(accountId);

    ITransport transport = _transportFactory(account, projectId, 60, null);
    var sendResult = await SendHelper.Send(commitObject, transport, true, null, ct).ConfigureAwait(false);

    ct.ThrowIfCancellationRequested();

    onOperationProgressed?.Invoke("Linking version to model...", null);

    using var apiClient = new Client(account);
    string versionId = await apiClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = projectId,
          branchName = modelId,
          sourceApplication = _revitSettings.HostSlug, // POC: These naming is a bit?
          objectId = sendResult.rootObjId
        },
        ct
      )
      .ConfigureAwait(true);

    return versionId;
  }
}
