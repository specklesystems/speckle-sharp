using Speckle.Connectors.DUI.Models;
using Speckle.Newtonsoft.Json;

namespace ConnectorArcGIS.Utils;

public class ArcGisDocumentStore : DocumentModelStore
{
  public ArcGisDocumentStore(JsonSerializerSettings serializerOption)
    : base(serializerOption)
  {
    // Subscribe here document related events like OnSave, OnClose, OnOpen etc...
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
