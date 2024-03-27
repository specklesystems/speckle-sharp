using ArcGIS.Desktop.Framework.Threading.Tasks;
using Speckle.Core.Models;

namespace Speckle.Connectors.ArcGis.Operations.Send;

//POC: This file is a copy-paste from Rhino

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
  public async Task<string> Execute(
    //ISendFilter sendFilter,
    string accountId,
    string projectId,
    string modelId,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    Base commitObject = await QueuedTask.Run(() => _baseBuilder.Build(onOperationProgressed, ct)).ConfigureAwait(false);

    // base object handler is separated so we can do some testing on non-production databases
    // exact interface may want to be tweaked when we implement this
    string versionId = await _baseObjectSender
      .Send(commitObject, accountId, projectId, modelId, onOperationProgressed, ct)
      .ConfigureAwait(false);

    return versionId;
  }
}
