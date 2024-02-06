using System.Collections.Generic;
using DUI3.Models;
using Rhino;

namespace ConnectorRhinoWebUI.Utils;

public class RhinoDocumentStore : DocumentModelStore
{
  private const string SPECKLE_KEY = "Speckle_DUI3";
  public override bool IsDocumentInit { get; set; } = true; // Note: because of rhino implementation details regarding expiry checking of sender cards.

  public RhinoDocumentStore()
  {
    // NOTE: IsDocumentInit setting does not work if rhino starts and opens a blank doc. I've commented out the relevant parts re change detection in the send binding
    RhinoDoc.BeginSaveDocument += (_, _) => WriteToFile();
    RhinoDoc.CloseDocument += (_, _) => WriteToFile();
    RhinoDoc.BeginOpenDocument += (_, _) => IsDocumentInit = false;
    RhinoDoc.EndOpenDocument += (_, e) =>
    {
      if (e.Merge)
      {
        return;
      }

      if (e.Document == null)
      {
        return;
      }

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

    RhinoDoc.ActiveDoc?.Strings.Delete(SPECKLE_KEY);

    string serializedState = Serialize();
    RhinoDoc.ActiveDoc?.Strings.SetString(SPECKLE_KEY, SPECKLE_KEY, serializedState);
  }

  public override void ReadFromFile()
  {
    string stateString = RhinoDoc.ActiveDoc.Strings.GetValue(SPECKLE_KEY, SPECKLE_KEY);
    if (stateString == null)
    {
      Models = new List<ModelCard>();
      return;
    }
    Models = Deserialize(stateString);
  }
}
