using Microsoft.Extensions.Logging;
using Speckle.Connectors.Utils.Builders;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Connectors.Utils.Operations;

public sealed class ReceiveOperation
{
  private readonly IHostObjectBuilder _hostObjectBuilder;
  private readonly ISyncToMainThread _syncToMainThread;
  private readonly ILogger<ReceiveOperation> _logger;

  public ReceiveOperation(
    IHostObjectBuilder hostObjectBuilder,
    ISyncToMainThread syncToMainThread,
    ILogger<ReceiveOperation> logger
  )
  {
    _hostObjectBuilder = hostObjectBuilder;
    _syncToMainThread = syncToMainThread;
    _logger = logger;
  }

  public async Task<IEnumerable<string>> Execute(
    string accountId, // POC: all these string arguments exists in ModelCard but not sure to pass this dependency here, TBD!
    string projectId,
    string projectName,
    string modelName,
    string versionId,
    CancellationToken cancellationToken,
    Action<string, double?>? onOperationProgressed = null
  )
  {
    // 2 - Check account exist
    Account account = AccountManager.GetAccount(accountId);

    // 3 - Get commit object from server
    using Client apiClient = new(account);
    Commit version = await apiClient.CommitGet(projectId, versionId, cancellationToken).ConfigureAwait(false);

    using ServerTransport transport = new(account, projectId);
    Base commitObject = await Speckle.Core.Api.Operations
      .Receive(version.referencedObject, transport, cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    cancellationToken.ThrowIfCancellationRequested();

    try
    {
      // 4 - Convert objects
      var x = await _syncToMainThread
        .RunOnThread(() =>
        {
          return _hostObjectBuilder.Build(
            commitObject,
            projectName,
            modelName,
            onOperationProgressed,
            cancellationToken
          );
        })
        .ConfigureAwait(false);

      return x ?? new List<string>();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "Error while receiving.");
      throw;
    }
  }
}
