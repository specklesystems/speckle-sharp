using System;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Revit.Bindings;

internal class ReceiveBinding : RevitBaseBinding, ICancelable
{
  public CancellationManager CancellationManager { get; } = new();

  public ReceiveBinding(RevitContext revitContext, DocumentModelStore store, IBridge bridge)
    : base("receiveBinding", store, bridge, revitContext) { }

  public void CancelReceive(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  public async void Receive(string modelCardId, string versionId)
  {
    try
    {
      //// 0 - Init cancellation token source -> Manager also cancel it if exist before
      //CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      //// 1 - Get receiver card
      //ReceiverModelCard model = _store.GetModelById(modelCardId) as ReceiverModelCard;

      //// 2 - Get commit object from server
      //Base commitObject = await Operations.GetCommitBase(Parent, model, versionId, cts.Token).ConfigureAwait(true);

      //if (cts.IsCancellationRequested)
      //{
      //  return;
      //}

      //// 3 - Get converter
      //ISpeckleConverter converter = Converters.GetConverter(Doc, RevitAppProvider.Version());

      //// 4 - Traverse commit object
      //List<Base> objectsToConvert = Traversal.GetObjectsToConvert(commitObject, converter);

      //// 5 - Bake objects
      //BakeObjects(objectsToConvert, converter, modelCardId, cts);
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
