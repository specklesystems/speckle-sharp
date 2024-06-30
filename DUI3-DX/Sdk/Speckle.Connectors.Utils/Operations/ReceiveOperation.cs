using Speckle.Connectors.Utils.Builders;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Serialisation.TypeCache;
using Speckle.Core.Transports;

namespace Speckle.Connectors.Utils.Operations;

public sealed class ReceiveOperation
{
  private readonly IHostObjectBuilder _hostObjectBuilder;
  private readonly ISyncToThread _syncToThread;
  private readonly ITypeCache _typeCache;

  public ReceiveOperation(IHostObjectBuilder hostObjectBuilder, ITypeCache typeCache, ISyncToThread syncToThread)
  {
    _hostObjectBuilder = hostObjectBuilder;
    _syncToThread = syncToThread;
    _typeCache = typeCache;
  }

  public async Task<HostObjectBuilderResult> Execute(
    string accountId, // POC: all these string arguments exists in ModelCard but not sure to pass this dependency here, TBD!
    string projectId,
    string projectName,
    string modelName,
    string versionId,
    ITypeCache typeCache,
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
      .Receive(version.referencedObject, typeCache, transport, cancellationToken: cancellationToken)
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
