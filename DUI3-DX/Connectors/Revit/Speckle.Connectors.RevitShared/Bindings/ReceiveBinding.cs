using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
  private readonly ReceiveOperation _receiveOperation;

  public ReceiveBinding(
    RevitContext revitContext,
    RevitDocumentStore store,
    IBridge bridge,
    ReceiveOperation receiveOperation
  )
    : base("receiveBinding", store, bridge, revitContext)
  {
    _receiveOperation = receiveOperation;
  }

  public void CancelReceive(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  public async Task Receive(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      if (_store.GetModelById(modelCardId) is not ReceiverModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      List<string> receivedObjectIds = (
        await _receiveOperation
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

  //private async void BakeObjects(
  //  List<Base> objectsToConvert,
  //  ISpeckleConverter converter,
  //  string modelCardId,
  //  CancellationTokenSource cts
  //)
  //{
  //  (bool success, Exception exception) = await RevitTask
  //    .RunAsync(app =>
  //    {
  //      string transactionName = $"Baking model from {modelCardId}";
  //      using TransactionGroup g = new(Doc, transactionName);
  //      using Transaction t = new(Doc, transactionName);
  //      g.Start();
  //      t.Start();

  //      try
  //      {
  //        converter.SetContextDocument(t);
  //        List<string> errors = new();
  //        int count = 0;
  //        foreach (Base objToConvert in objectsToConvert)
  //        {
  //          count++;
  //          if (cts.IsCancellationRequested)
  //          {
  //            Progress.CancelReceive(Parent, modelCardId, (double)count / objectsToConvert.Count);
  //            break;
  //          }
  //          try
  //          {
  //            double progress = (double)count / objectsToConvert.Count;
  //            Progress.ReceiverProgressToBrowser(Parent, modelCardId, progress);
  //            object convertedObject = converter.ConvertToNative(objToConvert);
  //            RefreshView();
  //          }
  //          catch (SpeckleException e)
  //          {
  //            errors.Add($"Object couldn't converted with id: {objToConvert.id}, type: {objToConvert.speckle_type}\n");
  //            Console.WriteLine(e);
  //          }
  //        }
  //        Notification.ReportReceive(Parent, errors, modelCardId, objectsToConvert.Count);

  //        t.Commit();

  //        if (t.GetStatus() == TransactionStatus.RolledBack)
  //        {
  //          int numberOfErrors = 0; // Previously get from errorEater
  //          return (
  //            false,
  //            new SpeckleException(
  //              $"The Revit API could not resolve {numberOfErrors} unique errors and {numberOfErrors} total errors when trying to commit the Speckle model. The whole transaction is being rolled back."
  //            )
  //          );
  //        }

  //        g.Assimilate();
  //        return (true, null);
  //      }
  //      catch (SpeckleException ex)
  //      {
  //        t.RollBack();
  //        g.RollBack();
  //        return (false, ex); //We can't throw exceptions in from RevitTask, but we can return it along with a success status
  //      }
  //    })
  //    .ConfigureAwait(false);
  //}

  //private void RefreshView()
  //{
  //  // regenerate the document and then implement a hack to "refresh" the view
  //  UiDoc.Document.Regenerate();

  //  // get the active ui view
  //  View view = UiDoc.ActiveGraphicalView ?? UiDoc.ActiveView;
  //  if (view is TableView)
  //  {
  //    return;
  //  }

  //  UIView uiView = UiDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

  //  // "refresh" the active view
  //  uiView?.Zoom(1);
  //}
}
