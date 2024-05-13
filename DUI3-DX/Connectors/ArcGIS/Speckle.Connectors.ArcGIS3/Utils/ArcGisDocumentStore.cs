using System.Xml.Linq;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.ArcGIS.Utils;

public class ArcGISDocumentStore : DocumentModelStore
{
  public ArcGISDocumentStore(JsonSerializerSettings serializerOption)
    : base(serializerOption)
  {
    ActiveMapViewChangedEvent.Subscribe(OnMapViewChanged);
    ProjectSavingEvent.Subscribe(OnProjectSaving);
    ProjectClosingEvent.Subscribe(OnProjectClosing);
  }

  private Task OnProjectClosing(ProjectClosingEventArgs arg)
  {
    if (MapView.Active is null)
    {
      return Task.CompletedTask;
    }

    WriteToFile();
    return Task.CompletedTask;
  }

  private Task OnProjectSaving(ProjectEventArgs arg)
  {
    if (MapView.Active is null)
    {
      return Task.CompletedTask;
    }

    WriteToFile();
    return Task.CompletedTask;
  }

  /// <summary>
  /// On map view switch, this event trigger twice, first for outgoing view, second for incoming view.
  /// </summary>
  private void OnMapViewChanged(ActiveMapViewChangedEventArgs args)
  {
    if (args.OutgoingView is not null)
    {
      WriteToFileWithMap(args.OutgoingView.Map);
    }

    if (args.IncomingView is not null)
    {
      IsDocumentInit = true;
      ReadFromFile();
      OnDocumentChanged();
    }
  }

  private void WriteToFileWithMap(Map map)
  {
    string serializedModels = Serialize();

    XDocument xmlDocument = new(new XElement("metadata", new XElement("SpeckleModelCards", serializedModels)));

    QueuedTask.Run(() =>
    {
      map.SetMetadata(xmlDocument.ToString());
    });
  }

  public override void WriteToFile() => WriteToFileWithMap(MapView.Active.Map);

  public override void ReadFromFile()
  {
    Map map = MapView.Active.Map;
    QueuedTask.Run(() =>
    {
      var metadata = map.GetMetadata();
      var root = XDocument.Parse(metadata).Root;
      var element = root?.Element("SpeckleModelCards");
      if (element is null)
      {
        Models = new List<ModelCard>();
        return;
      }

      string modelsString = element.Value;
      Models = Deserialize(modelsString);
    });
  }
}
