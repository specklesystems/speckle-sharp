using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Speckle.Core.Models;
using Revit.Async;
using DUI3.Utils;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SendBinding : ISendBinding
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }
  
  private RevitDocumentStore _store;

  private static UIApplication RevitApp;

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
    SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    Document doc = RevitApp.ActiveUIDocument.Document;

    List<Element> elements = objectsIds.Select(x => doc.GetElement(x)).Where(x => x != null).ToList();

    var converter = KitManager.GetDefaultKit().LoadConverter("Revit2023");
    converter.SetContextDocument(doc);

    var convertedObjects = new List<Base>();

    await RevitTask
        .RunAsync(_ =>
        {
          int count = 0;
          foreach (var revitElement in elements)
          {
            count++;
            convertedObjects.Add(converter.ConvertToSpeckle(revitElement));
            double progress = (double)count / elements.Count;
            Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
          }
        })
        .ConfigureAwait(false);

    var commitObject = new Base();
    commitObject["@elements"] = convertedObjects;

    var projectId = model.ProjectId;
    Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
    var client = new Client(account);

    var transports = new List<ITransport> { new ServerTransport(client.Account, projectId) };

    // TODO: Fix send operations haven't succeeded
    var objectId = await Operations.Send(
      commitObject,
      transports,
      disposeTransports: true
    ).ConfigureAwait(true);

    Parent.SendToBrowser(SendBindingEvents.CreateVersion, new CreateVersion() { AccountId = account.id, ModelId = model.ModelId, ProjectId = model.ProjectId, ObjectId = objectId, Message = "Test", SourceApplication = "Revit" });
  }

  public void CancelSend(string modelId)
  {
    throw new System.NotImplementedException();
  }

  public void Highlight(string modelId)
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
