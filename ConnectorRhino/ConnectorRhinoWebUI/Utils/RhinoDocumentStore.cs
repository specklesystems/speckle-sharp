using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using DUI3.Models;
using Rhino;

namespace ConnectorRhinoWebUI.Utils;

public class RhinoDocumentStore : DocumentModelStore
{
  private const string SpeckleKey = "Speckle_DUI3";
  public override bool IsDocumentInit { get; set; } = true; // Note: because of rhino implementation details regarding expiry checking of sender cards.
  
  public RhinoDocumentStore()
  {
    RhinoDoc.BeginSaveDocument += (_, _) => WriteToFile();
    RhinoDoc.CloseDocument += (_, _) => WriteToFile();
    RhinoDoc.BeginOpenDocument += (_, _) => IsDocumentInit = false;
    RhinoDoc.EndOpenDocument += (_, e) =>
    {
      if (e.Merge) return;
      if (e.Document == null) return;
      
      IsDocumentInit = true;
      ReadFromFile();
      OnDocumentChanged();
    };
  }

  public override void WriteToFile()
  {
    if (RhinoDoc.ActiveDoc == null)
    {
      return; // Should throw
    }
    RhinoDoc.ActiveDoc?.Strings.Delete(SpeckleKey);
    var serializedState = Serialize();
    
    RhinoDoc.ActiveDoc?.Strings.SetString(SpeckleKey, SpeckleKey, serializedState);
  }

  public override void ReadFromFile()
  {
    var stateString = RhinoDoc.ActiveDoc.Strings.GetValue(SpeckleKey, SpeckleKey);
    if (stateString == null)
    {
      Models = new List<ModelCard>();
      return;
    }
    Models = Deserialize(stateString);
  }
}
