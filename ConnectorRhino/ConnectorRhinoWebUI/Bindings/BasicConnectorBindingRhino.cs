using System.Collections.Generic;
using System.Linq;
using DUI3;
using DUI3.Bindings;
using Rhino;
using Speckle.Core.Credentials;

namespace ConnectorRhinoWebUI.Bindings;

public class BasicConnectorBindingRhino : IBasicConnectorBinding<RhinoSenderCard>
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

  public void CreateSenderCard(RhinoSenderCard senderCard)
  {
    // TODO
    var card = senderCard; // serialization test
    var test = senderCard.Settings.Filter;
  }

  public DocumentState GetDocumentState()
  {
    return _documentState;
  }

  // public void AddModelToDocumentState(ModelCard model)
  // {
  //   _documentState.Models.Add(model);
  // }
  //
  // public void RemoveModelFromDocumentState(ModelCard model)
  // {
  //   var index = _documentState.Models.FindIndex(m => m.Id == model.Id);
  //   _documentState.Models.RemoveAt(index);
  // }

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

  public void Send(RhinoSenderCard m)
  {
    var objs = m.Filter.GetObjects();
  }
  
  public object GetAvailableFilterTypes()
  {
    return new object()
    {
      "Selection": nameof(RhinoSelectionFilter),
      "List": nameof(RhinoLayerFilter)
    }
  }
  
}

public class RhinoSenderCard : ISenderCard<RhinoSendSettings, RhinoSendFilter>
{
  public string Id { get; set; }
  public string ModelId { get; set; }
  public string ProjectId { get; set; }
  public string AccountId { get; set; }
  public string LastLocalUpdate { get; set; }
  public string Type { get; set; }
  public RhinoSendSettings Settings { get; set; }
  public RhinoSendFilter Filter { get; set; }

  public RhinoSenderCard() {}
}

public class RhinoSendSettings : ISendSettings
{
  // TODO
  
}

// public class RhinoSendFilter : ISendFilter
// {
//   public string Name { get; set; }
//   public string Summary { get; set; }
// }

public class _Rhino_SelectionFilter : ISendFilter
{
  public string Type { get; set; } = nameof(RhinoSelectionFilter);
  public new string Name { get; set; } = "Selection";
  public new string Summary { get; set; }

  public void GetObjects()
  {
   // LOGIC HERE 
  }
}

public class RhinoLayerFilter : RhinoSendFilter
{
  public string Type { get; set; } = nameof(RhinoLayerFilter);
  public new string Name { get; set; } = "Layers";
  public new string Summary { get; set; }
}
