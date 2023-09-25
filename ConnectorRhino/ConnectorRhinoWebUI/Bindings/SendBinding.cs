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
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Objects.Converter.RhinoGh;
using DUI3.Utils;

namespace ConnectorRhinoWebUI.Bindings;

public class SendBinding : ISendBinding, ICancellable
{
  public string Name { get; set; } = "sendBinding";
  private static string ApplicationIdKey = "applicationId";
  public IBridge Parent { get; set; }
  private DocumentModelStore _store;

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

  private async void SendProgress(string modelCardId, double progress)
  {
    var args = new ModelProgress()
    {
      Id = modelCardId,
      Status = progress == 1 ? "Completed" : "Converting",
      Progress = progress
    };
    Parent.SendToBrowser(SendBindingEvents.SenderProgress, args);
  }

  public async void Send(string modelCardId)
  {
    if (CancellationManager.IsExist(modelCardId))
    {
      CancellationManager.CancelOperation(modelCardId);
    }
    var cts = CancellationManager.InitCancellationTokenSource(modelCardId);
    
    RhinoDoc doc = RhinoDoc.ActiveDoc;
    SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    
    // Collect RhinoObjects from their guids
    IEnumerable<RhinoObject> rhinoObjects = objectsIds.Select((id) => doc.Objects.FindId(new Guid(id)));

    ConverterRhinoGh converter = new ConverterRhinoGh();
    converter.SetContextDocument(doc);

    var convertedObjects = new List<Base>();
    int count = 0;
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      if (cts.IsCancellationRequested) return;
      count++;
      convertedObjects.Add(converter.ConvertToSpeckle(rhinoObject));
      double progress = (double)count / objectsIds.Count;
      Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
      Thread.Sleep(5000);
    }
    
    if (CancellationManager.IsCancellationRequested(modelCardId)) return;
    var commitObject = new Base();
    commitObject["@elements"] = convertedObjects;

    var projectId = model.ProjectId;
    Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
    var client = new Client(account);

    var transports = new List<ITransport> { new ServerTransport(client.Account, projectId) };

    var objectId = await Speckle.Core.Api.Operations.Send(
      commitObject,
      cts.Token,
      transports,
      disposeTransports: true
    ).ConfigureAwait(true);

    Parent.SendToBrowser(SendBindingEvents.CreateVersion, new CreateVersion() { ModelCardId = modelCardId, AccountId = account.id, ModelId = model.ModelId, ProjectId = model.ProjectId, ObjectId = objectId, Message = "Test", SourceApplication = "Rhino" });
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

  
}
