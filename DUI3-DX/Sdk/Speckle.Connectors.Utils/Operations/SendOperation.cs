using Speckle.Connectors.Utils.Builders;

namespace Speckle.Connectors.Utils.Operations;

public sealed class SendOperation<T>
{
  private readonly IRootObjectBuilder<T> _rootObjectBuilder;
  private readonly IRootObjectSender _baseObjectSender;
  private readonly ISyncToThread _syncToThread;

  public SendOperation(
    IRootObjectBuilder<T> rootObjectBuilder,
    IRootObjectSender baseObjectSender,
    ISyncToThread syncToThread
  )
  {
    _rootObjectBuilder = rootObjectBuilder;
    _baseObjectSender = baseObjectSender;
    _syncToThread = syncToThread;
  }

  public async Task<SendOperationResult> Execute(
    IReadOnlyList<T> objects,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  )
  {
    var results = await _syncToThread
      .RunOnThread(() => _rootObjectBuilder.Build(objects, sendInfo, onOperationProgressed, ct))
      .ConfigureAwait(false);

    // base object handler is separated so we can do some testing on non-production databases
    // exact interface may want to be tweaked when we implement this
    var (rootObjId, convertedReferences) = await _baseObjectSender
      .Send(results.Root, sendInfo, onOperationProgressed, ct)
      .ConfigureAwait(false);

    return new(results, rootObjId, convertedReferences);
  }
}
