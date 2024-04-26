using System;
using Speckle.Core.Models;
using System.Threading.Tasks;
using System.Threading;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Operations.Send;

public sealed class SendOperation
{
  private readonly RootObjectBuilder _rootObjectBuilder;
  private readonly IRootObjectSender _rootObjectSender;

  public SendOperation(RootObjectBuilder rootObjectBuilder, IRootObjectSender rootObjectSender)
  {
    _rootObjectBuilder = rootObjectBuilder;
    _rootObjectSender = rootObjectSender;
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
    // POC: have changed this as I don't understand the injecting of the ISendFilter when we can just use it here
    // it begs the question whether ISendFilter should just be injected into the roo object builder and whether this function needs it at all?
    // this class is now so thing I wonder if it should exist at all?
    Base commitObject = _rootObjectBuilder.Build(
      new SendSelection(sendFilter.GetObjectIds()),
      onOperationProgressed,
      ct
    );

    return await _rootObjectSender
      .Send(commitObject, accountId, projectId, modelId, onOperationProgressed, ct)
      .ConfigureAwait(true);
  }
}
