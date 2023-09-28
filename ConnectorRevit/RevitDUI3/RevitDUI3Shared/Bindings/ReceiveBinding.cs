using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using DUI3.Utils;
using DUI3.Operations;
using Revit.Async;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorRevitDUI3.Bindings
{
  public class ReceiveBinding : IBinding, ICancelable
  {
    public string Name { get; set; } = "receiveBinding";
    public IBridge Parent { get; set; }
    private readonly RevitDocumentStore _store;
    private static UIApplication _revitApp;
    
    private static Document Doc => UiDoc.Document;
    private static UIDocument UiDoc => _revitApp.ActiveUIDocument;

    public CancellationManager CancellationManager { get; } = new();

    public ReceiveBinding(RevitDocumentStore store)
    {
      _revitApp = RevitAppProvider.RevitApp;
      _store = store;
    }
    
    public void CancelReceive(string modelCardId)
    {
      CancellationManager.CancelOperation(modelCardId);
    }

    public async void Receive(string modelCardId, string versionId)
    {
      try
      {
        // 0 - Init cancellation token source -> Manager also cancel it if exist before
        var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

        // 1 - Get receiver card
        ReceiverModelCard model = _store.GetModelById(modelCardId) as ReceiverModelCard;
      
        // 2 - Get commit object from server
        Base commitObject = await Operations.GetCommitBase(Parent, model, versionId, cts.Token).ConfigureAwait(true);
      
        if (cts.IsCancellationRequested) return;
      
        // 3 - Get converter
        ISpeckleConverter converter = Converters.GetConverter(Doc, RevitAppProvider.Version());
      
        // 4 - Traverse commit object
        List<Base> objectsToConvert = Traversal.GetObjectsToConvert(commitObject, converter);
      
        // 5 - Bake objects
        BakeObjects(objectsToConvert, converter, modelCardId, cts);
      }
      catch (Exception e)
      {
        if (e is OperationCanceledException)
        {
          Progress.CancelReceive(Parent, modelCardId);
          return;
        }
        throw;
      }
    }

    private async void BakeObjects(List<Base> objectsToConvert, ISpeckleConverter converter, string modelCardId, CancellationTokenSource cts)
    {
      (bool success, Exception exception) = await RevitTask.RunAsync(
        app =>
        {
          string transactionName = $"Baking model from {modelCardId}";
          using var g = new TransactionGroup(Doc, transactionName);
          using var t = new Transaction(Doc, transactionName);
          g.Start();
          t.Start();

          try
          {
            converter.SetContextDocument(t);
            var errors = new List<string>();
            int count = 0;
            foreach (var objToConvert in objectsToConvert)
            {
              count++;
              if (cts.IsCancellationRequested)
              {
                Progress.CancelReceive(Parent, modelCardId, (double)count / objectsToConvert.Count);
                break;
              }
              try
              {
                double progress = (double)count / objectsToConvert.Count;
                Progress.ReceiverProgressToBrowser(Parent, modelCardId, progress);
                var convertedObject = converter.ConvertToNative(objToConvert);
                RefreshView();
              }
              catch (Exception e)
              {
                errors.Add($"Object couldn't converted with id: {objToConvert.id}, type: {objToConvert.speckle_type}\n");
                Console.WriteLine(e);
              }
            }
            Notification.ReportReceive(Parent, errors, modelCardId, objectsToConvert.Count);

            t.Commit();

            if (t.GetStatus() == TransactionStatus.RolledBack)
            {
              int numberOfErrors = 0; // Previously get from errorEater
              return (false,
                new SpeckleException(
                  $"The Revit API could not resolve {numberOfErrors} unique errors and {numberOfErrors} total errors when trying to commit the Speckle model. The whole transaction is being rolled back."));
            }

            g.Assimilate();
            return (true, null);
          }
          catch (Exception ex)
          {
            t.RollBack();
            g.RollBack();
            return
              (false, ex); //We can't throw exceptions in from RevitTask, but we can return it along with a success status
          }
        }).ConfigureAwait(false);
    }
    
    private void RefreshView()
    {
      // regenerate the document and then implement a hack to "refresh" the view
      UiDoc.Document.Regenerate();

      // get the active ui view
      var view = UiDoc.ActiveGraphicalView ?? UiDoc.ActiveView;
      if (view is TableView)
      {
        return;
      }

      var uiView = UiDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

      // "refresh" the active view
      uiView?.Zoom(1);
    }
  }
}
