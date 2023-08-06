using System.Collections.Generic;
using System.Linq;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI.Bindings;

public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  private DocumentModelStore _store;

  public BasicConnectorBinding(DocumentModelStore store)
  {
    _store = store;
    RhinoDoc.BeginSaveDocument += (_, _) => WriteDocState();
    RhinoDoc.CloseDocument += (_, _) => WriteDocState();
    RhinoDoc.EndOpenDocument += (_, e) =>
    {
      if (e.Merge) return;
      if (e.Document == null) return;
      Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
    };
  }

  public string GetSourceApplicationName()
  {
    return "Rhino";
  }

  public string GetSourceApplicationVersion()
  {
    return "7";
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
    return _store;
  }

  public void AddModel(ModelCard model)
  {
    _store.Models.Add(model);
  }

  public void UpdateModel(ModelCard model)
  {
     var idx = _store.Models.FindIndex(m => model.Id == m.Id);
    _store.Models[idx] = model;
  }
  
  public void RemoveModel(ModelCard model)
  {
    var index = _store.Models.FindIndex(m => m.Id == model.Id);
    _store.Models.RemoveAt(index);
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
    var serializedState = _store.Serialize();
    
    RhinoDoc.ActiveDoc?.Strings.SetString(SpeckleKey, SpeckleKey, serializedState);
  }
  
  /// <summary>
  /// Populates the _documentState from the current document info.
  /// </summary>
  private void ReadDocState()
  {
    // TODO: clean up
    var stateString = RhinoDoc.ActiveDoc.Strings.GetValue(SpeckleKey, SpeckleKey);
    if (stateString == null)
    {
      _store.Models = new List<ModelCard>();
      return;
    }
    var state = DocumentModelStore.Deserialize(stateString);
    _store.Models = state.Models;
  }
}
