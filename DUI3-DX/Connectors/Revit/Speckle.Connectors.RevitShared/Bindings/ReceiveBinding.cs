using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Revit.Bindings;

internal class ReceiveBinding : RevitBaseBinding, ICancelable
{
  public CancellationManager CancellationManager { get; } = new();
  private readonly IUnitOfWorkFactory _unitOfWorkFactory;

  public ReceiveBinding(
    RevitContext revitContext,
    RevitDocumentStore store,
    IBridge bridge,
    IUnitOfWorkFactory unitOfWorkFactory
  )
    : base("receiveBinding", store, bridge, revitContext)
  {
    _unitOfWorkFactory = unitOfWorkFactory;
  }

  public void CancelReceive(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  public async Task Receive(string modelCardId)
  {
    try
    {
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      if (_store.GetModelById(modelCardId) is not ReceiverModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      using var receiveOperaion = _unitOfWorkFactory.Resolve<ReceiveOperation>();

      List<string> receivedObjectIds = (
        await receiveOperaion.Service
          .Execute(
            modelCard.AccountId,
            modelCard.ProjectId,
            modelCard.ProjectName,
            modelCard.ModelName,
            modelCard.SelectedVersionId,
            cts.Token,
            null
          )
          .ConfigureAwait(false)
      ).ToList();
    }
    catch (Exception e) when (!e.IsFatal())
    {
      //if (e is OperationCanceledException)
      //{
      //  Progress.CancelReceive(Parent, modelCardId);
      //  return;
      //}
      //throw;
    }
  }
}
