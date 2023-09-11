using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Utils;
using Objects.Converter.Revit;
using Revit.Async;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.ConnectorRevitDUI3.Bindings
{
  public class ReceiveBinding : IBinding
  {
    public string Name { get; set; } = "receiveBinding";
    public IBridge Parent { get; set; }
    private RevitDocumentStore _store;
    private static UIApplication RevitApp;

    public ReceiveBinding(RevitDocumentStore store)
    {
      RevitApp = RevitAppProvider.RevitApp;
      _store = store;
    }
    
    public void CancelReceive(string modelCardId)
    {
      throw new NotImplementedException();
    }

    public async void Receive(string modelCardId, string versionId)
    {
      Document doc = RevitApp.ActiveUIDocument.Document;
      Base commitObject = await DUI3.Utils.Receive.GetCommitBase(Parent, _store, modelCardId, versionId);
      
      var (success, exception) = await RevitTask.RunAsync(app =>
      {
        string transactionName = $"Baking model from {modelCardId}";
        using var g = new TransactionGroup(doc, transactionName);
        using var t = new Transaction(doc, transactionName);

        var converter = new ConverterRevit();
        converter.SetContextDocument(doc);

        var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

        var objectsToConvert = traverseFunction
          .Traverse(commitObject)
          .Select(
            tc => tc.current) // Previously we were creating ApplicationObject, now just returning Base object.
          .Reverse()
          .ToList();

        g.Start();
        t.Start();

        try
        {
          converter.SetContextDocument(t);

          foreach (var objectToConvert in objectsToConvert)
          {
            try
            {
              var convertedObject = converter.ConvertToNative(objectToConvert);
              RefreshView();
            }
            catch (Exception e)
            {
              Console.WriteLine(e);
            }
          }
          
          t.Commit();

          if (t.GetStatus() == TransactionStatus.RolledBack)
          {
            int numberOfErrors = 0; // Previously get from errorEater
            return (false, new SpeckleException($"The Revit API could not resolve {numberOfErrors} unique errors and {numberOfErrors} total errors when trying to commit the Speckle model. The whole transaction is being rolled back."));
          }

          g.Assimilate();
          return (true, null);
        }
        catch (Exception ex)
        {
          t.RollBack();
          g.RollBack();
          return (false, ex); //We can't throw exceptions in from RevitTask, but we can return it along with a success status
        }
      }).ConfigureAwait(false);
    }
    
    private void RefreshView()
    {
      UIDocument doc = RevitApp.ActiveUIDocument;
      //regenerate the document and then implement a hack to "refresh" the view
      doc.Document.Regenerate();

      // get the active ui view
      var view = doc.ActiveGraphicalView ?? doc.ActiveView;
      if (view is TableView)
      {
        return;
      }

      var uiView = doc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

      // "refresh" the active view
      uiView.Zoom(1);
    }
  }
}
