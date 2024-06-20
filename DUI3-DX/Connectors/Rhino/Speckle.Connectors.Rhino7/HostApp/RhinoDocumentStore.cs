using Rhino;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.Rhino7.HostApp;

public class RhinoDocumentStore : DocumentModelStore
{
  private readonly TopLevelExceptionHandler _topLevelExceptionHandler;
  private const string SPECKLE_KEY = "Speckle_DUI3";
  public override bool IsDocumentInit { get; set; } = true; // Note: because of rhino implementation details regarding expiry checking of sender cards.

  public RhinoDocumentStore(
    JsonSerializerSettings jsonSerializerSettings,
    TopLevelExceptionHandler topLevelExceptionHandler
  )
    : base(jsonSerializerSettings, true)
  {
    _topLevelExceptionHandler = topLevelExceptionHandler;
    RhinoDoc.BeginOpenDocument += (_, _) => topLevelExceptionHandler.CatchUnhandled(() => IsDocumentInit = false);
    RhinoDoc.EndOpenDocument += (_, e) =>
      topLevelExceptionHandler.CatchUnhandled(() =>
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
      });
  }

  public override void WriteToFile()
  {
    if (RhinoDoc.ActiveDoc == null)
    {
      return; // Should throw
    }

    RhinoDoc.ActiveDoc.Strings.Delete(SPECKLE_KEY);

    string serializedState = Serialize();
    RhinoDoc.ActiveDoc.Strings.SetString(SPECKLE_KEY, SPECKLE_KEY, serializedState);
  }

  public override void ReadFromFile()
  {
    string stateString = RhinoDoc.ActiveDoc.Strings.GetValue(SPECKLE_KEY, SPECKLE_KEY);
    if (stateString == null)
    {
      Models = new();
      return;
    }
    Models = Deserialize(stateString) ?? new();
  }
}
