using System;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Models;

namespace Speckle.Connectors.Rhino7.Operations.Send;

public sealed class SendOperation
{
  private readonly RootBaseObjectBuilder _baseBuilder;
  private readonly IBaseObjectSender _baseObjectSender;

  public SendOperation(RootBaseObjectBuilder baseBuilder, IBaseObjectSender baseObjectSender)
  {
    _baseBuilder = baseBuilder;
    _baseObjectSender = baseObjectSender;
  }

  public async Task Execute(
    ISendFilter sendFilter,
    string accountId,
    string projectId,
    string modelId,
    Action<string, double?>? onOperationProgressed = null,
    Action<string>? onVersionIdCreated = null,
    CancellationToken ct = default
  )
  {
    Base commitObject = _baseBuilder.Build(sendFilter, onOperationProgressed, ct);

    // base object handler is separated so we can do some testing on non-production databases
    // exact interface may want to be tweaked when we implement this
    string versionId = await _baseObjectSender
      .Send(commitObject, accountId, projectId, modelId, onOperationProgressed, ct)
      .ConfigureAwait(false);

    onVersionIdCreated?.Invoke(versionId);
  }
}
