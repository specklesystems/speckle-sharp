using System;
using Speckle.Core.Models;
using System.Threading.Tasks;
using System.Threading;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Connectors.Revit.Operations.Send;

public sealed class SendOperation
{
  // POC: this now feels like a layer of nothing and the caller should be instantiating the things it needs, maybe...
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;
  private readonly IRootObjectSender _rootObjectSender;

  public SendOperation(IUnitOfWorkFactory unitOfWorkFactory, IRootObjectSender rootObjectSender)
  {
    _unitOfWorkFactory = unitOfWorkFactory;
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
    using (var rootObjectBuilder = _unitOfWorkFactory.Resolve<RootObjectBuilder>())
    {
      // POC: have changed this as I don't understand the injecting of the ISendFilter when we can just use it here
      // it begs the question whether ISendFilter should just be injected into the roo object builder and whether this function needs it at all?
      // this class is now so thing I wonder if it should exist at all?
      // everything is being passed in from the caller? It feels like the caller should be instantiating the UoW
      commitObject = rootObjectBuilder.Service.Build(
        new SendSelection(sendFilter.GetObjectIds()),
        onOperationProgressed,
        ct
      );
    }

    return await _rootObjectSender
      .Send(commitObject, accountId, projectId, modelId, onOperationProgressed, ct)
      .ConfigureAwait(true);
  }
}
