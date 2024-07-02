using Speckle.Connectors.Utils.Builders;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Connectors.Utils.Operations;

public sealed class ReceiveOperation
{
  private readonly IHostObjectBuilder _hostObjectBuilder;
  private readonly ISyncToThread _syncToThread;
  private readonly AccountService _accountService;

  public ReceiveOperation(
    IHostObjectBuilder hostObjectBuilder,
    ISyncToThread syncToThread,
    AccountService accountService
  )
  {
    _hostObjectBuilder = hostObjectBuilder;
    _syncToThread = syncToThread;
    _accountService = accountService;
  }

  public async Task<HostObjectBuilderResult> Execute(
    string accountId, // POC: all these string arguments exists in ModelCard but not sure to pass this dependency here, TBD!
    Uri serverUrl,
    string projectId,
    string projectName,
    string modelName,
    string versionId,
    CancellationToken cancellationToken,
    Action<string, double?>? onOperationProgressed = null
  )
  {
    // 2 - Check account exist
    Account account = _accountService.GetAccountWithServerUrlFallback(accountId, serverUrl);

    // 3 - Get commit object from server
    using Client apiClient = new(account);
    Commit version = await apiClient.CommitGet(projectId, versionId, cancellationToken).ConfigureAwait(false);

    using ServerTransport transport = new(account, projectId);
    Base commitObject = await Speckle.Core.Api.Operations
      .Receive(version.referencedObject, transport, cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    cancellationToken.ThrowIfCancellationRequested();

    // 4 - Convert objects
    return await _syncToThread
      .RunOnThread(() =>
      {
        return _hostObjectBuilder.Build(commitObject, projectName, modelName, onOperationProgressed, cancellationToken);
      })
      .ConfigureAwait(false);
  }
}
