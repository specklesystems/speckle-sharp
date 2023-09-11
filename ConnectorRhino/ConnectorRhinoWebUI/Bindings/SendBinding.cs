using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Objects.Converter.RhinoGh;
using DUI3.Utils;
using Layer = Rhino.DocObjects.Layer;

namespace ConnectorRhinoWebUI.Bindings;

public class SendBinding : ISendBinding
{
  public string Name { get; set; } = "sendBinding";
  private static string ApplicationIdKey = "applicationId";
  public IBridge Parent { get; set; }
  private DocumentModelStore _store;

  private HashSet<string> _changedObjectIds { get; set; } = new();
  
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
      _changedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };
    
    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      if (!_store.IsDocumentInit) return;
      _changedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };
    
    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      if (!_store.IsDocumentInit) return;
      _changedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      _changedObjectIds.Add(e.OldRhinoObject.Id.ToString());
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

  public IDictionary<Layer, IEnumerable<object>> GroupByLayer(IEnumerable<RhinoObject> rhinoObjects, RhinoDoc doc)
  {
    IDictionary<Layer, IEnumerable<RhinoObject>> objsByLayer = rhinoObjects.GroupBy(o => doc.Layers.FindIndex(o.Attributes.LayerIndex)).ToDictionary(key => key.Key, value => value.AsEnumerable());
    IDictionary<Layer, List<object>> objectByNestedLayers = new Dictionary<Layer, List<object>>();

    foreach (KeyValuePair<Layer, IEnumerable<RhinoObject>> layerWithObjects in objsByLayer)
    {
      Layer layer = layerWithObjects.Key;
      List<object> objects = layerWithObjects.Value as List<object>;

      List<Layer> layerTree = new List<Layer>() { layer };

      bool layerIsChild = layer.ParentLayerId != Guid.Empty;
      while (layerIsChild)
      {
        layer = doc.Layers.FindId(layer.ParentLayerId);
        layerIsChild = layer.ParentLayerId != Guid.Empty;
        layerTree.Add(layer);
      }

      IDictionary<Layer, List<object>> nestedLayers = new Dictionary<Layer, List<object>>();
      foreach (Layer node in layerTree)
      {
        nestedLayers = new Dictionary<Layer, List<object>>();
        nestedLayers.Add(node, objects);
        objects = new List<object> { node };
      }
    }

    return objectByNestedLayers.ToDictionary(k => k.Key, v => v.Value.AsEnumerable());
  }

  private async void SendProgress(string modelCardId, double progress)
  {
    var args = new SenderProgress()
    {
      Id = modelCardId,
      Status = progress == 1 ? "Completed" : "Converting",
      Progress = progress
    };
    Parent.SendToBrowser(SendBindingEvents.SenderProgress, args);
  }

  public async void Send(string modelCardId)
  {
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
      count++;
      convertedObjects.Add(converter.ConvertToSpeckle(rhinoObject));
      double progress = (double)count / objectsIds.Count;
      Dispatcher.CurrentDispatcher.Invoke(() =>
      {
        Progress.SenderProgressToBrowser(Parent, modelCardId, progress);          
      }, DispatcherPriority.Background);
    }

    var commitObject = new Base();
    commitObject["@elements"] = convertedObjects;

    var projectId = model.ProjectId;
    Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
    var client = new Client(account);

    var transports = new List<ITransport> { new ServerTransport(client.Account, projectId) };

    var objectId = await Speckle.Core.Api.Operations.Send(
      commitObject,
      transports,
      disposeTransports: true
    ).ConfigureAwait(true);

    Parent.SendToBrowser(SendBindingEvents.CreateVersion, new CreateVersion() { AccountId = account.id, ModelId = model.ModelId, ProjectId = model.ProjectId, ObjectId = objectId, Message = "Test", SourceApplication = "Rhino" });
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
      if (isExpired)
      {
        expiredSenderIds.Add(sender.Id);
      }
    }
    Parent.SendToBrowser(SendBindingEvents.SendersExpired, expiredSenderIds);
    _changedObjectIds = new HashSet<string>();
  }
}
