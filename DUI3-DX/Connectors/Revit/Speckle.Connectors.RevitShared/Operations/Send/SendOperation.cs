using System;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Models;
using System.Threading.Tasks;
using System.Threading;
using Autofac;

namespace Speckle.Connectors.Revit.Operations.Send;

public sealed class SendOperation
{
  private readonly ILifetimeScope _scope;
  private readonly IRootObjectSender _rootObjectSender;

  public SendOperation(ILifetimeScope scope, IRootObjectSender rootObjectSender)
  {
    _scope = scope;
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
    Base commitObject;
    using (var scope = _scope.BeginLifetimeScope())
    {
      RootObjectBuilder rootObjectBuilder = scope.Resolve<Func<ISendFilter, RootObjectBuilder>>()(sendFilter);
      commitObject = rootObjectBuilder.Build(onOperationProgressed, ct);
    }

    return await _rootObjectSender
      .Send(commitObject, accountId, projectId, modelId, onOperationProgressed, ct)
      .ConfigureAwait(true);
  }
}
