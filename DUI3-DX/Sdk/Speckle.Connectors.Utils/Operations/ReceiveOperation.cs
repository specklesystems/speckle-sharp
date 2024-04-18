using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Connectors.Utils.Builders;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Connectors.Utils.Operations;

public sealed class ReceiveOperation
{
  private readonly IHostObjectBuilder _hostObjectBuilder;

  public ReceiveOperation(IHostObjectBuilder hostObjectBuilder)
  {
    _hostObjectBuilder = hostObjectBuilder;
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
    Account account =
      AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == accountId)
      ?? throw new SpeckleAccountManagerException();

    // 3 - Get commit object from server
    using Client apiClient = new(account);
    Commit version = await apiClient.CommitGet(projectId, versionId, cancellationToken).ConfigureAwait(true);

    using ServerTransport transport = new(account, projectId);
    Base commitObject = await Speckle.Core.Api.Operations
      .Receive(version.referencedObject, transport, cancellationToken: cancellationToken)
      .ConfigureAwait(true);

    cancellationToken.ThrowIfCancellationRequested();

    // 4 - Convert objects
    return _hostObjectBuilder.Build(commitObject, projectName, modelName, onOperationProgressed, cancellationToken);
  }
}
