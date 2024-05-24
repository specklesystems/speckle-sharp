using System.Xml.Linq;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.ArcGIS.Utils;

public class ArcGISDocumentStore : DocumentModelStore
{
  public ArcGISDocumentStore(JsonSerializerSettings serializerOption)
    : base(serializerOption, true)
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
    if (args.IncomingView is null)
    {
      return;
    }

    IsDocumentInit = true;
    ReadFromFile();
    OnDocumentChanged();
  }

  public override void WriteToFile()
  {
    Map map = MapView.Active.Map;
    QueuedTask.Run(() =>
    {
      // Read existing metadata - To prevent messing existing metadata. 🤞 Hope other add-in developers will do same :D
      var existingMetadata = map.GetMetadata();

      // Parse existing metadata
      XDocument existingXmlDocument = !string.IsNullOrEmpty(existingMetadata)
        ? XDocument.Parse(existingMetadata)
        : new XDocument(new XElement("metadata"));

      string serializedModels = Serialize();

      XElement xmlModelCards = new("SpeckleModelCards", serializedModels);

      // Check if SpeckleModelCards element already exists at root and update it
      var speckleModelCardsElement = existingXmlDocument.Root?.Element("SpeckleModelCards");
      if (speckleModelCardsElement != null)
      {
        speckleModelCardsElement.ReplaceWith(xmlModelCards);
      }
      else
      {
        existingXmlDocument.Root?.Add(xmlModelCards);
      }

      map.SetMetadata(existingXmlDocument.ToString());
    });
  }

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
        Models = new();
        return;
      }

      string modelsString = element.Value;
      Models = Deserialize(modelsString).NotNull();
    });
  }
}
