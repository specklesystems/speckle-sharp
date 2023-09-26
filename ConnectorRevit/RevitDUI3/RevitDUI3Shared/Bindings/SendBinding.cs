using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using DUI3;
using DUI3.Bindings;
using DUI3.Operations;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Speckle.Core.Models;
using Revit.Async;
using DUI3.Utils;
using Operations = Speckle.Core.Api.Operations;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }

  private RevitDocumentStore _store;

  private static UIApplication RevitApp;

  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> _changedObjectIds { get; set; } = new();

  public SendBinding(RevitDocumentStore store)
  {
    RevitApp = RevitAppProvider.RevitApp;
    _store = store;

    // TODO expiry events
    // TODO filters need refresh events
    RevitApp.Application.DocumentChanged += (_, e) => DocChangeHandler(e);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>
    {
      new RevitEverythingFilter(),
      new RevitSelectionFilter()
    };
  }

  public async void Send(string modelCardId)
  {
    try
    {
      if (CancellationManager.IsExist(modelCardId))
      {
        CancellationManager.CancelOperation(modelCardId);
      }

      var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
      // TODO: Here send warning to UI if somehow model return null!
      List<string> objectsIds = model.SendFilter.GetObjectIds();
      Document doc = RevitApp.ActiveUIDocument.Document;

      List<Element> elements = objectsIds.Select(x => doc.GetElement(x)).Where(x => x != null).ToList();

      var converter = KitManager.GetDefaultKit().LoadConverter("Revit2023");
      converter.SetContextDocument(doc);

      var convertedObjects = new List<Base>();

      // TBD: RevitTask complicates cancellation operation. Do we really need it after new
      // Bridge implementation with threading?
      // If we remove this RevitTask.RunAsync, we don't need to wrap all into try catch
      await RevitTask
        .RunAsync(
          _ =>
          {
            int count = 0;
            foreach (var revitElement in elements)
            {
              if (cts.IsCancellationRequested)
              {
                Progress.CancelSend(Parent, modelCardId, (double)count / objectsIds.Count);
                return;
              }

              count++;
              convertedObjects.Add(converter.ConvertToSpeckle(revitElement));
              double progress = (double)count / elements.Count;
              Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
            }
          })
        .ConfigureAwait(false);
      if (cts.IsCancellationRequested)
      {
        Progress.CancelSend(Parent, modelCardId);
        return;
      }
      
      var commitObject = new Base();
      commitObject["@elements"] = convertedObjects;

      var projectId = model.ProjectId;
      Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
      var client = new Client(account);

      var transports = new List<ITransport> { new ServerTransport(client.Account, projectId) };

      // TODO: Fix send operations haven't succeeded
      // Pass null progress value to let UI swooshing progress bar
      Progress.SerializerProgressToBrowser(Parent, modelCardId, null);
      var objectId = await Operations.Send(
          commitObject,
          cts.Token,
          transports,
          disposeTransports: true
        )
        .ConfigureAwait(true);
      // Pass 1 progress value to let UI finish progress
      Progress.SerializerProgressToBrowser(Parent, modelCardId, 1);
      if (cts.IsCancellationRequested) return;

      Parent.SendToBrowser(
        SendBindingEvents.CreateVersion,
        new CreateVersion()
        {
          AccountId = account.id,
          ModelId = model.ModelId,
          ModelCardId = modelCardId,
          ProjectId = model.ProjectId,
          ObjectId = objectId,
          Message = "Test",
          SourceApplication = "Revit"
        });
    }
    catch (Exception e)
    {
      if (e is OperationCanceledException)
      {
        Progress.CancelSend(Parent, modelCardId);
      }
    }
  }

  public void CancelSend(string modelCardId)
  {
    CancellationManager.CancelOperation(modelCardId);
  }

  public void Highlight(string modelCardId)
  {
    throw new System.NotImplementedException();
  }

  /// <summary>
  /// Keeps track of the changed element ids as well as checks if any of them need to trigger
  /// a filter refresh (e.g., views being added). 
  /// </summary>
  /// <param name="e"></param>
  private void DocChangeHandler(Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
  {
    var addedElementIds = e.GetAddedElementIds();
    var deletedElementIds = e.GetDeletedElementIds();
    var modifiedElementIds = e.GetModifiedElementIds();
    var doc = e.GetDocument();

    foreach (ElementId elementId in addedElementIds)
    {
      _changedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in deletedElementIds)
    {
      _changedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in modifiedElementIds)
    {
      _changedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    // TODO: CHECK IF ANY OF THE ABOVE ELEMENTS NEED TO TRIGGER A FILTER REFRESH

    RevitIdleManager.SubscribeToIdle(RunExpirationChecks);
  }

  private void RunExpirationChecks()
  {
    var senders = _store.GetSenders();
    var expiredSenderIds = new List<string>();

    foreach (var sender in senders)
    {
      var isExpired = sender.SendFilter.CheckExpiry(_changedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(sender.Id);
      }
    }

    Parent.SendToBrowser(SendBindingEvents.SendersExpired, expiredSenderIds);
    _changedObjectIds = new HashSet<string>();
  }
}
