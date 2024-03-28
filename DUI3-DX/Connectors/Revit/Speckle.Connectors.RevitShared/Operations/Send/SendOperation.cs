using System;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Models;
using System.Threading.Tasks;
using System.Threading;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using System.Linq;
using Autofac;

namespace Speckle.Connectors.Revit.Operations.Send;

public sealed class SendOperation
{
  private readonly ILifetimeScope _scope;

  public SendOperation(ILifetimeScope scope)
  {
    _scope = scope;
  }

  /// <summary>
  /// Executes a send operation given information about the host objects and the destination account.
  /// </summary>
  /// <param name="sendFilter"></param>
  /// <param name="accountId"></param>
  /// <param name="projectId"></param>
  /// <param name="modelId"></param>
  /// <param name="onOperationProgressed"></param>
  /// <param name="ct"></param>
  /// <returns></returns>
  public async Task<string> Execute(
    ISendFilter sendFilter,
    string accountId,
    string projectId,
    string modelId,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    Base commitObject;
    using (var scope = _scope.BeginLifetimeScope())
    {
      RootObjectBuilder rootObjectBuilder = scope.Resolve<Func<ISendFilter, RootObjectBuilder>>()(sendFilter);
      commitObject = rootObjectBuilder.Build(onOperationProgressed, ct);
    }

    Account account =
      AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == accountId)
      ?? throw new SpeckleAccountManagerException();

    var transport = new ServerTransport(account, projectId);
    var sendResult = await SendHelper.Send(commitObject, transport, true, null, ct).ConfigureAwait(true);

    var apiClient = new Client(account);
    string versionId = await apiClient
      .CommitCreate(
        new CommitCreateInput
        {
          streamId = projectId,
          branchName = modelId,
          sourceApplication = "Revit",
          objectId = sendResult.rootObjId
        },
        ct
      )
      .ConfigureAwait(true);

    return versionId;
  }
}
