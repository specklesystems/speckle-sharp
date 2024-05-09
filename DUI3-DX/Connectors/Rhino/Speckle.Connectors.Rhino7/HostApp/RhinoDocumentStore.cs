using Rhino;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Rhino7.HostApp;

public class RhinoDocumentStore : DocumentModelStore
{
  private const string SPECKLE_KEY = "Speckle_DUI3";
  public override bool IsDocumentInit { get; set; } = true; // Note: because of rhino implementation details regarding expiry checking of sender cards.

  public RhinoDocumentStore(JsonSerializerSettings jsonSerializerSettings)
    : base(jsonSerializerSettings)
  {
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
    Models = Deserialize(stateString) ?? new ();
  }
}
