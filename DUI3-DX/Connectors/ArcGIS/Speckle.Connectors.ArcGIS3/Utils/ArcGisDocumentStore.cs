using ArcGIS.Desktop.Mapping.Events;
using Speckle.Connectors.DUI.Models;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.ArcGIS.Utils;

public class ArcGISDocumentStore : DocumentModelStore
{
  public ArcGISDocumentStore(JsonSerializerSettings serializerOption)
    : base(serializerOption)
  {
    // POC: Subscribe here document related events like OnSave, OnClose, OnOpen etc...
    ActiveMapViewChangedEvent.Subscribe(OnMapViewChanged);
  }

  private void OnMapViewChanged(ActiveMapViewChangedEventArgs args)
  {
    OnDocumentChanged();
  }

  public override void WriteToFile()
  {
    // Implement the logic to save it to file
  }

  public override void ReadFromFile()
  {
    // Implement the logic to read it from file
  }
}
