using System;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Models;

namespace Speckle.Connectors.Rhino7.Operations.Send;

/// <summary>
/// Stateless send operation orchestrator.
/// </summary>
public sealed class SendOperation
{
  private readonly RootBaseObjectBuilder _baseBuilder;
  private readonly IBaseObjectSender _baseObjectSender;

  public SendOperation(RootBaseObjectBuilder baseBuilder, IBaseObjectSender baseObjectSender)
  {
    _baseBuilder = baseBuilder;
    _baseObjectSender = baseObjectSender;
  }

  /// <summary>
  /// Executes a send operation given information about the host objects and the destination account.
  /// </summary>
  /// <param name="sendFilter"></param>
  /// <param name="accountId"></param>
  /// <param name="projectId"></param>
  /// <param name="modelId"></param>
  /// <param name="onOperationProgressed"></param>
  /// <param name="onVersionIdCreated"></param>
  /// <param name="ct"></param>
  /// <returns></returns>
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
