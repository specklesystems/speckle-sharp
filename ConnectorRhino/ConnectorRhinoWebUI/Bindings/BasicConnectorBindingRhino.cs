using System.Collections.Generic;
using System.Linq;
using DUI3;
using DUI3.Bindings;
using Rhino;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI.Bindings;

public class BasicConnectorBindingRhino : IBasicConnectorBinding
{
  public string Name { get; set; } = "baseBinding";
  public IBridge Parent { get; set; }
  private DocumentState _documentState;
  
  public BasicConnectorBindingRhino()
  {
    RhinoDoc.BeginSaveDocument += (_, _) => WriteDocState();
    RhinoDoc.CloseDocument += (_, _) => WriteDocState();
    RhinoDoc.EndOpenDocumentInitialViewUpdate += (sender, e) =>
    {
      if (e.Merge) return;
      if (e.Document == null) return;
      Parent?.SendToBrowser(BasicConnectorBindingEvents.DocumentChanged);
      ReadDocState();
    };

    var test = new RhinoEverythingFilter();

    var name = test.TypeDiscriminator;
    var yolo = test.GetType().FullName;
    
    _documentState = new DocumentState();
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
  
  public void SaveDocumentState(DocumentState state)
  {
    _documentState = state;
    WriteDocState();
  }
  
  public void GetAvailableFilters()
  {
    // TODO: think and implement, and this should go in the binding defintion.
    var selectionFilter = new RhinoSelectionFilter();
    var everythingFilter = new RhinoEverythingFilter();
  }
  
  public DocumentState GetDocumentState()
  {
    return _documentState;
  }

  public void AddModelToDocumentState(ModelCard model)
  {
    _documentState.Models.Add(model);
  }

  public void UpdateModelInDocumentState(ModelCard model)
  {
    // TODO: implement
  }
  
  public void RemoveModelFromDocumentState(ModelCard model)
  {
    var index = _documentState.Models.FindIndex(m => m.Id == model.Id);
    _documentState.Models.RemoveAt(index);
  }

  private const string SpeckleKey = "Speckle_DUI3";
  /// <summary>
  /// Writes the _documentState to the current document info.
  /// </summary>
  private void WriteDocState()
  {
    RhinoDoc.ActiveDoc?.Strings.Delete(SpeckleKey);
    var serializedState = _documentState.Serialize();
    RhinoDoc.ActiveDoc?.Strings.SetString(SpeckleKey, _documentState.Serialize());
  }
  
  /// <summary>
  /// Populates the _documentState from the current document info.
  /// </summary>
  private void ReadDocState()
  {
    var strings = RhinoDoc.ActiveDoc.Strings.GetEntryNames(SpeckleKey);
    if(strings==null || strings.Length < 1) return;
    var state = DocumentState.Deserialize(strings[0]);
    _documentState = state;
  }
}

public class RhinoEverythingFilter : SendFilter
{
  public override List<string> GetObjectIds()
  {
    // TODO 
    return new List<string>();
  }
}

public class RhinoSelectionFilter : SendFilter
{
  public List<string> ObjectIds { get; set; } = new List<string>();
  public override List<string> GetObjectIds()
  {
    return ObjectIds;
  }
}

public class RhinoSendSettings : SendSettings
{
  public bool CleanBreps { get; set; } = false;
}

