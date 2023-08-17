using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Objects.Converter.RhinoGh;
using CefSharp.DevTools.LayerTree;
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

  public async void Send(string modelCardId)
  {
    RhinoDoc doc = RhinoDoc.ActiveDoc;
    SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    
    // Collect RhinoObjects from their guids
    IEnumerable<RhinoObject> rhinoObjects = objectsIds.Select((id) => doc.Objects.FindId(new Guid(id)));
    // Group RhinoObjects according to their layers
    // TODO: Handle here nested layers!
    IDictionary<Layer, IEnumerable<RhinoObject>> objsByLayer = rhinoObjects.GroupBy(o => doc.Layers.FindIndex(o.Attributes.LayerIndex)).ToDictionary(key => key.Key, value => value.AsEnumerable());

    IDictionary<Layer, List<object>> objectByNestedLayers = new Dictionary<Layer, List<object>>();


    IDictionary<Layer, IEnumerable<object>> objsByLayerTest = GroupByLayer(rhinoObjects, doc);

    // Collect named views
    IEnumerable<object> namedViews = objectsIds
      .Select(id => doc.NamedViews.FindByName(id))
      .Where(index => index != -1)
      .Select(index => doc.NamedViews[index]);
    // Collect views
    IEnumerable<ViewInfo> views = objectsIds
      .Where(id => doc.Views.Find(new Guid(id)) != null)
      .Select(id => doc.Views.Find(new Guid(id)).ActiveViewport)
      .Select(viewport => new ViewInfo(viewport));
    
    ConverterRhinoGh converter = new ConverterRhinoGh();
    converter.SetContextDocument(doc);

    var commitObject = converter.ConvertToSpeckle(doc) as Collection; // create a collection base obj

    foreach (KeyValuePair<Layer, IEnumerable<RhinoObject>> layerWithObjects in objsByLayer)
    {
      var layerObject = converter.ConvertToSpeckle(layerWithObjects.Key) as Collection;
      foreach (RhinoObject rhinoObject in layerWithObjects.Value)
      {
        layerObject.elements.Add(converter.ConvertToSpeckle(rhinoObject));
      }
      commitObject.elements.Add(layerObject);
    }


    /*
    // ------- DUI2 WAY --------
    var converter = KitManager.GetDefaultKit().LoadConverter(Utils.Utils.RhinoAppName);
    converter.SetContextDocument(doc);

    var streamId = model.ProjectId;
    Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
    var client = new Client(account);

    int objCount = objectsIds.Count;
    int count = 0;

    // var commitObject = converter.ConvertToSpeckle(doc) as Collection; // create a collection base obj


    // store converted commit objects and layers by layer paths
    var commitLayerObjects = new Dictionary<string, List<Base>>();
    var commitLayers = new Dictionary<string, Layer>();
    var commitCollections = new Dictionary<string, Collection>();

    // convert all commit objs
    foreach (var selectedId in objectsIds)
    {
      Base converted = null;
      string applicationId = null;
      var reportObj = new ApplicationObject(selectedId, "Unknown");
      if (Utils.Utils.FindObjectBySelectedId(doc, selectedId, out object obj, out string descriptor))
      {
        // create applicationObject
        reportObj = new ApplicationObject(selectedId, descriptor);
        converter.Report.Log(reportObj); // Log object so converter can access
        switch (obj)
        {
          case RhinoObject o:
            applicationId = o.Attributes.GetUserString(ApplicationIdKey) ?? selectedId;
            if (!converter.CanConvertToSpeckle(o))
            {
              reportObj.Update(
                status: ApplicationObject.State.Skipped,
                logItem: "Sending this object type is not supported in Rhino"
              );
              // progress.Report.Log(reportObj);
              continue;
            }

            converted = converter.ConvertToSpeckle(o);

            if (converted != null)
            {
              var objectLayer = doc.Layers[o.Attributes.LayerIndex];
              if (commitLayerObjects.ContainsKey(objectLayer.FullPath))
                commitLayerObjects[objectLayer.FullPath].Add(converted);
              else
                commitLayerObjects.Add(objectLayer.FullPath, new List<Base> { converted });
              if (!commitLayers.ContainsKey(objectLayer.FullPath))
                commitLayers.Add(objectLayer.FullPath, objectLayer);
            }
            break;
          case Layer o:
            applicationId = o.GetUserString(ApplicationIdKey) ?? selectedId;
            converted = converter.ConvertToSpeckle(o);
            if (converted is Collection layerCollection && !commitLayers.ContainsKey(o.FullPath))
            {
              commitLayers.Add(o.FullPath, o);
              commitCollections.Add(o.FullPath, layerCollection);
            }
            break;
          case ViewInfo o:
            converted = converter.ConvertToSpeckle(o);
            if (converted != null)
              commitObject.elements.Add(converted);
            break;
        }
      }
      else
      {
        // progress.Report.LogOperationError(new Exception($"Failed to find doc object ${selectedId}."));
        continue;
      }

      if (converted == null)
      {
        reportObj.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null");
        // progress.Report.Log(reportObj);
        continue;
      }


      // Send here progress to UI!!
      count++;
      Parent.SendToBrowser(SendBindingEvents.SenderProgress, new SenderProgress() { Id = model.Id, Progress = count });
      // conversionProgressDict["Conversion"]++;
      // progress.Update(conversionProgressDict);

      // set application ids, also set for speckle schema base object if it exists
      converted.applicationId = applicationId;
      if (converted["@SpeckleSchema"] != null)
      {
        var newSchemaBase = converted["@SpeckleSchema"] as Base;
        newSchemaBase.applicationId = applicationId;
        converted["@SpeckleSchema"] = newSchemaBase;
      }

      // log report object
      reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {converted.speckle_type}");
      // progress.Report.Log(reportObj);

      objCount++;
    }

    #region layer handling
    // convert layers as collections and attach all layer objects
    foreach (var layerPath in commitLayerObjects.Keys)
      if (commitCollections.ContainsKey(layerPath))
      {
        commitCollections[layerPath].elements = commitLayerObjects[layerPath];
      }
      else
      {
        var collection = converter.ConvertToSpeckle(commitLayers[layerPath]) as Collection;
        if (collection != null)
        {
          collection.elements = commitLayerObjects[layerPath];
          commitCollections.Add(layerPath, collection);
        }
      }

    // generate all parent paths of commit collections and create ordered list by depth descending
    var allPaths = new HashSet<string>();
    foreach (var key in commitLayers.Keys)
    {
      if (!allPaths.Contains(key))
        allPaths.Add(key);
      AddParent(commitLayers[key]);

      void AddParent(Layer childLayer)
      {
        var parentLayer = doc.Layers.FindId(childLayer.ParentLayerId);
        if (parentLayer != null && !commitCollections.ContainsKey(parentLayer.FullPath))
        {
          var parentCollection = converter.ConvertToSpeckle(parentLayer) as Collection;
          if (parentCollection != null)
          {
            commitCollections.Add(parentLayer.FullPath, parentCollection);
            allPaths.Add(parentLayer.FullPath);
          }
          AddParent(parentLayer);
        }
      }
    }
    var orderedPaths = allPaths.OrderByDescending(path => path.Count(c => c == ':')).ToList(); // this ensures we attach children collections first

    // attach children collections to their parents and the base commit
    for (int i = 0; i < orderedPaths.Count; i++)
    {
      var path = orderedPaths[i];
      var collection = commitCollections[path];
      var parentIndex = path.LastIndexOf(Layer.PathSeparator);

      // if there is no parent, attach to base commit layer prop directly
      if (parentIndex == -1)
      {
        commitObject.elements.Add(collection);
        continue;
      }

      // get the parent collection, attach child, and update parent collection in commit collections
      var parentPath = path.Substring(0, parentIndex);
      var parent = commitCollections[parentPath];
      parent.elements.Add(commitCollections[path]);
      commitCollections[parentPath] = parent;
    }

    #endregion

    // progress.CancellationToken.ThrowIfCancellationRequested();

    // progress.Max = objCount;

    */

    var projectId = model.ProjectId;
    Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
    var client = new Client(account);

    var transports = new List<ITransport> { new ServerTransport(client.Account, projectId) };

    var objectId = await Operations.Send(
      commitObject,
      transports,
      disposeTransports: true
    ).ConfigureAwait(true);

    Parent.SendToBrowser(SendBindingEvents.CreateVersion, new CreateVersion() { AccountId = account.id, ModelId = model.ModelId, ProjectId = model.ProjectId, ObjectId = objectId, Message = "Test", HostApp = "Rhino" });
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
