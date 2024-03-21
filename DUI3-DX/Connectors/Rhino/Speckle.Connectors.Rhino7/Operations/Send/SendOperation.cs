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
  private readonly RootObjectBuilder _baseBuilder;
  private readonly IRootObjectSender _baseObjectSender;

  public SendOperation(RootObjectBuilder baseBuilder, IRootObjectSender baseObjectSender)
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
    Base commitObject = _baseBuilder.Build(sendFilter, onOperationProgressed, ct);

    // base object handler is separated so we can do some testing on non-production databases
    // exact interface may want to be tweaked when we implement this
    string versionId = await _baseObjectSender
      .Send(commitObject, accountId, projectId, modelId, onOperationProgressed, ct)
      .ConfigureAwait(false);

    return versionId;
  }
}
