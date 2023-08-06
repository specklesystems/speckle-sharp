using System.Collections.Generic;
using System.Linq;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;

namespace ConnectorRhinoWebUI.Bindings;

public class SendBinding : ISendBinding
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }
  private DocumentModelStore _store;
  private bool _runExpirationChecks = false;
  private HashSet<string> _changedObjectIds { get; set; } = new();
  
  public SendBinding(DocumentModelStore store)
  {
    _store = store;
    bool isDocInit = false;
    RhinoDoc.BeginOpenDocument += (_, _) => isDocInit = false;
    RhinoDoc.EndOpenDocument += (_, _) => isDocInit = true;
    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      Parent?.SendToBrowser(SendBindingEvents.FiltersNeedRefresh);
    };
    
    RhinoDoc.AddRhinoObject += (sender, e) =>
    {
      if (!isDocInit) return;
      _changedObjectIds.Add(e.ObjectId.ToString());
      _runExpirationChecks = true;
    };
    
    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      if (!isDocInit) return;
      _changedObjectIds.Add(e.ObjectId.ToString());
      _runExpirationChecks = true;
    };
    
    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      if (!isDocInit) return;
      _changedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      _changedObjectIds.Add(e.OldRhinoObject.Id.ToString());
      _runExpirationChecks = true;
    };
    
    RhinoApp.Idle += (_, _) => RunExpirationChecks();
  }
  
  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>()
    {
      new RhinoEverythingFilter(),
      new RhinoSelectionFilter(),
      new RhinoLayerFilter()
    };
  }

  public void Send(string modelId)
  {
    var model = _store.GetModelById(modelId) as SenderModelCard;
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
  
  private void RunExpirationChecks()
  {
    if(!_runExpirationChecks) return;
    var senders = _store.GetSenders();
    var objectIdsList = _changedObjectIds.ToArray();
    var expiredSenderIds = new List<string>();
    
    foreach (var sender in senders)
    {
      var isExpired = sender.SendFilter.CheckExpiry(objectIdsList);
      if(isExpired) expiredSenderIds.Add(sender.Id);
    }
    Parent.SendToBrowser(SendBindingEvents.SendersExpired, expiredSenderIds);
    _changedObjectIds = new HashSet<string>();
    _runExpirationChecks = false;
  }
}
