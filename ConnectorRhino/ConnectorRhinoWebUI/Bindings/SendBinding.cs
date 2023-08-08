using System;
using System.Collections.Generic;
using System.Linq;
using ConnectorRhinoWebUI.Utils;
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

  private HashSet<string> _changedObjectIds { get; set; } = new();
  
  public SendBinding(DocumentModelStore store)
  {
    _store = store;
    bool isDocInit = false;
    // TODO: TBD -> isDocInit always false for newly opened documents. For saved documents turns to the true. 
    //  If we somehow want to make sure that doc is initialized, maybe it should be passed here directly as referenced.
    //  So document events should be tracked with some other class and this class should? have the responsibilty update it's
    //  Doc property, so we won't need to check here since we will have already updated reference...
    RhinoDoc.BeginOpenDocument += (_, _) => isDocInit = false;
    RhinoDoc.EndOpenDocument += (_, _) => isDocInit = true;
    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      Parent?.SendToBrowser(SendBindingEvents.FiltersNeedRefresh);
    };
    
    RhinoDoc.AddRhinoObject += (_, e) =>
    {
      if (!isDocInit) return;
      _changedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(() => RunExpirationChecks());
    };
    
    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      if (!isDocInit) return;
      _changedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(() => RunExpirationChecks());
    };
    
    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      if (!isDocInit) return;
      _changedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      _changedObjectIds.Add(e.OldRhinoObject.Id.ToString());
      RhinoIdleManager.SubscribeToIdle(() => RunExpirationChecks());
    }; 
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
  }
}
