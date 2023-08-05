using System.Collections.Generic;
using System.Linq;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI.Bindings;

public class BasicConnectorBindingRhino : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  private DocumentModelStore _documentModelStore;

  public BasicConnectorBindingRhino(DocumentModelStore modelStore)
  {
    RhinoDoc.BeginSaveDocument += (_, _) => WriteDocState();
    RhinoDoc.CloseDocument += (_, _) => WriteDocState();
    RhinoDoc.EndOpenDocument += (_, e) =>
    {
      if (e.Merge) return;
      if (e.Document == null) return;
      Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
    };
    
    // NOTE: this fires every time for each layer that is modified in a bulk layer change operation.
    // We might wanna debounce it.
    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      Parent?.SendToBrowser(BasicConnectorBindingEvents.FiltersNeedRefresh);
    };
    _documentModelStore = modelStore;
  }

  public string GetSourceApplicationName()
  {
    return "Rhino";
  }

  public string GetSourceApplicationVersion()
  {
    return "7";
  }

  public Account[] GetAccounts()
  {
    return AccountManager.GetAccounts().ToArray();
  }

  public DocumentInfo GetDocumentInfo()
  {
    return new DocumentInfo
    {
      Location = RhinoDoc.ActiveDoc.Path,
      Name = RhinoDoc.ActiveDoc.Name,
      Id = RhinoDoc.ActiveDoc.RuntimeSerialNumber.ToString()
    };
  }

  public DocumentModelStore GetDocumentState()
  {
    ReadDocState();
    return _documentModelStore;
  }

  public void AddModelToDocumentState(ModelCard model)
  {
    _documentModelStore.Models.Add(model);
    WriteDocState();
  }

  public void UpdateModelInDocumentState(ModelCard model)
  {
     var idx = _documentModelStore.Models.FindIndex(m => model.Id == m.Id);
    _documentModelStore.Models[idx] = model;
    WriteDocState();
  }
  
  public void RemoveModelFromDocumentState(ModelCard model)
  {
    var index = _documentModelStore.Models.FindIndex(m => m.Id == model.Id);
    _documentModelStore.Models.RemoveAt(index);
    WriteDocState();
  }

  public List<SendFilter> GetSendFilters()
  {
    return new List<SendFilter>()
    {
      new RhinoEverythingFilter() { Name = "Everything" },
      new RhinoSelectionFilter() { Name = "Selection" },
      new RhinoLayerFilter() { Name = "Layers" }
    };
  }

  private const string SpeckleKey = "Speckle_DUI3";
  /// <summary>
  /// Writes the _documentState to the current document info.
  /// </summary>
  private void WriteDocState()
  {
    if (RhinoDoc.ActiveDoc == null)
    {
      return; // Should throw
    }
    RhinoDoc.ActiveDoc?.Strings.Delete(SpeckleKey);
    var serializedState = _documentModelStore.Serialize();
    
    RhinoDoc.ActiveDoc?.Strings.SetString(SpeckleKey, SpeckleKey, serializedState);
  }
  
  /// <summary>
  /// Populates the _documentState from the current document info.
  /// </summary>
  private void ReadDocState()
  {
    var stateString = RhinoDoc.ActiveDoc.Strings.GetValue(SpeckleKey, SpeckleKey);
    if (stateString == null)
    {
      _documentModelStore = new DocumentModelStore();
      return;
    }
    var state = DocumentModelStore.Deserialize(stateString);
    _documentModelStore = state;
  }
}

// internal class ExpirationChecker
// {
//   private bool _needToRun = false;
//   private HashSet<string> objectIds = new HashSet<string>();
//   private BasicConnectorBindingRhino _parent;
//   public ExpirationChecker(BasicConnectorBindingRhino parent)
//   {
//     _parent = parent;
//     RhinoDoc.AddRhinoObject += (sender, e) =>
//     {
//       objectIds.Add(e.ObjectId.ToString());
//       _needToRun = true;
//     };
//     RhinoDoc.DeleteRhinoObject += (_, e) =>
//     {
//       objectIds.Add(e.ObjectId.ToString());
//       _needToRun = true;
//     };
//     RhinoDoc.ReplaceRhinoObject += (_, e) =>
//     {
//       objectIds.Add(e.NewRhinoObject.Id.ToString());
//       objectIds.Add(e.OldRhinoObject.Id.ToString());
//       _needToRun = true;
//     };
//     RhinoApp.Idle += (_, _) => CheckAndNotifyExpiration();
//   }
//
//   public void CheckAndNotifyExpiration()
//   {
//     if(!_needToRun) return;
//     var senders = _parent._documentState.Models.FindAll(f => f.TypeDiscriminator == nameof(SenderModelCard)).Cast<SenderModelCard>().ToList();
//     var senderscount = senders.Count;
//     var objectIdsList = objectIds.ToArray();
//     var expiredSenderIds = new List<string>();
//     
//     // etc. Do work
//     foreach (var sender in senders)
//     {
//       var isExpired= sender.SendFilter.CheckExpiry(objectIdsList);
//       if(isExpired) expiredSenderIds.Add(sender.Id);
//     }
//     
//     // TODO notify browser
//     objectIds = new HashSet<string>();
//     _needToRun = false;
//   }
// }
