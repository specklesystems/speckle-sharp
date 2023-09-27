using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Operations;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using DUI3.Utils;
using Speckle.Core.Kits;

namespace ConnectorRhinoWebUI.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }
  
  private readonly DocumentModelStore _store;

  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> ChangedObjectIds { get; set; } = new();
  
  public SendBinding(DocumentModelStore store)
  {
    _store = store;
    
    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      Parent?.SendToBrowser(SendBindingEvents.FiltersNeedRefresh);
    };
    
    RhinoDoc.AddRhinoObject += (_, e) =>
    {
      if (!_store.IsDocumentInit) return;
      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };
    
    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      if (!_store.IsDocumentInit) return;
      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };
    
    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      if (!_store.IsDocumentInit) return;
      ChangedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      ChangedObjectIds.Add(e.OldRhinoObject.Id.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
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
      List<RhinoObject> rhinoObjects = GetObjectsFromDocument(model);

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(RhinoDoc.ActiveDoc, "Rhino7");

      // 5 - Convert objects
      Base commitObject = ConvertObjects(rhinoObjects, converter, modelCardId, cts);
      
      if (cts.IsCancellationRequested) return;

      // 6 - Get transports
      var transports = new List<ITransport> { new ServerTransport(account, model.ProjectId) };

      // 7 - Serialize and Send objects
      string objectId = await Operations.Send(Parent, modelCardId, commitObject, cts.Token, transports).ConfigureAwait(true);
      
      if (cts.IsCancellationRequested) return;

      // 8 - Create Version
      Operations.CreateVersion(Parent, model, objectId, "Rhino");
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
  
  private void RunExpirationChecks()
  {
    var senders = _store.GetSenders();
    var objectIdsList = ChangedObjectIds.ToArray();
    var expiredSenderIds = new List<string>();
    
    foreach (var sender in senders)
    {
      var isExpired = sender.SendFilter.CheckExpiry(objectIdsList);
      if (isExpired)
      {
        expiredSenderIds.Add(sender.Id);
      }
    }
    Parent.SendToBrowser(SendBindingEvents.SendersExpired, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  private Base ConvertObjects(List<RhinoObject> rhinoObjects, ISpeckleConverter converter, string modelCardId, CancellationTokenSource cts)
  {
    var commitObject = new Base();
    
    var convertedObjects = new List<Base>();
    int count = 0;
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      if (cts.IsCancellationRequested)
      {
        Progress.CancelSend(Parent, modelCardId, (double)count / rhinoObjects.Count);
        break;
      }
      
      count++;
      convertedObjects.Add(converter.ConvertToSpeckle(rhinoObject));
      double progress = (double)count / rhinoObjects.Count;
      Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
    }
    
    commitObject["@elements"] = convertedObjects;

    return commitObject;
  }

  private List<RhinoObject> GetObjectsFromDocument(SenderModelCard model)
  {
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    return objectsIds.Select((id) => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id))).ToList();
  }
}
