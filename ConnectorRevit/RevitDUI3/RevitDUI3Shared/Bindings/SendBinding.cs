using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorRevitDUI3.Utils;

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

  public void Send(string modelId)
  {
    throw new System.NotImplementedException();
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
