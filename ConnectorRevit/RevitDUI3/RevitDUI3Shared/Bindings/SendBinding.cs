using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using DUI3;
using DUI3.Bindings;
using DUI3.Operations;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Speckle.Core.Models;
using DUI3.Utils;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }

  private readonly RevitDocumentStore _store;

  private static UIApplication _revitApp;

  private static Document Doc => _revitApp.ActiveUIDocument.Document;

  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public SendBinding(RevitDocumentStore store)
  {
    _revitApp = RevitAppProvider.RevitApp;
    _store = store;

    // TODO expiry events
    // TODO filters need refresh events
    _revitApp.Application.DocumentChanged += (_, e) => DocChangeHandler(e);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>
    {
      new RevitEverythingFilter(),
      new RevitSelectionFilter()
    };
  }

  private List<Element> GetObjectsFromDocument(SenderModelCard model)
  {
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    return objectsIds
      .Select(x => _revitApp.ActiveUIDocument.Document.GetElement(x))
      .Where(x => x != null).ToList();
  }

  private Base ConvertElements(
    List<Element> elements,
    ISpeckleConverter converter,
    string modelCardId,
    CancellationTokenSource cts
  )
  {
    var commitObject = new Base();
    
    var convertedObjects = new List<Base>();
    int count = 0;
    foreach (var revitElement in elements)
    {
      if (cts.IsCancellationRequested)
      {
        Progress.CancelSend(Parent, modelCardId, (double)count / elements.Count);
        // throw new OperationCanceledException(); TBD -> Not sure
        break;
      }

      count++;
      convertedObjects.Add(converter.ConvertToSpeckle(revitElement));
      double progress = (double)count / elements.Count;
      Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
    }
    
    commitObject["@elements"] = convertedObjects;

    return commitObject;
  }

  public async void Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
      
      // 2 - Check account exist
      Account account = Accounts.GetAccount(model.AccountId);
      
      // 3 - Get elements to convert
      List<Element> elements = GetObjectsFromDocument(model);

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(Doc, RevitAppProvider.Version());

      // 5 - Convert objects
      Base commitObject = ConvertElements(elements, converter, modelCardId, cts);
      
      if (cts.IsCancellationRequested) return;

      // 6 - Get transports
      var transports = new List<ITransport> { new ServerTransport(account, model.ProjectId) };

      // 7 - Serialize and Send objects
      string objectId = await Operations.Send(Parent, modelCardId, commitObject, cts.Token, transports).ConfigureAwait(true);
      
      if (cts.IsCancellationRequested) return;

      // 8 - Create Version
      Operations.CreateVersion(Parent, model, objectId, "Revit");
    }
    catch (Exception e)
    {
      if (e is OperationCanceledException)
      {
        Progress.CancelSend(Parent, modelCardId);
        return;
      }
      // TODO: Init here class to handle send errors to report UI, Seq etc..
      throw;
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

    foreach (ElementId elementId in addedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in deletedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
    }

    foreach (ElementId elementId in modifiedElementIds)
    {
      ChangedObjectIds.Add(elementId.IntegerValue.ToString());
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
      var isExpired = sender.SendFilter.CheckExpiry(ChangedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(sender.Id);
      }
    }

    Parent.SendToBrowser(SendBindingEvents.SendersExpired, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }
}
