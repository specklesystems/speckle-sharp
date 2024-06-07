using System.Reflection;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Reflection;

namespace Speckle.Connectors.ArcGIS.Bindings;

//poc: dupe code between connectors
public class BasicConnectorBinding : IBasicConnectorBinding
{
  public string Name => "baseBinding";
  public IBridge Parent { get; }

  public BasicConnectorBindingCommands Commands { get; }
  private readonly DocumentModelStore _store;
  private readonly ArcGISSettings _settings;

  public BasicConnectorBinding(DocumentModelStore store, ArcGISSettings settings, IBridge parent)
  {
    _store = store;
    _settings = settings;
    Parent = parent;
    Commands = new BasicConnectorBindingCommands(parent);

    _store.DocumentChanged += (_, _) =>
    {
      Commands.NotifyDocumentChanged();
    };
  }

  public string GetSourceApplicationName() => _settings.HostAppInfo.Slug;

  public string GetSourceApplicationVersion() => _settings.HostAppInfo.GetVersion(_settings.HostAppVersion);

  public string GetConnectorVersion() => Assembly.GetAssembly(GetType()).NotNull().GetVersion();

  public DocumentInfo? GetDocumentInfo()
  {
    if (MapView.Active is null)
    {
      return null;
    }

    return new DocumentInfo(MapView.Active.Map.URI, MapView.Active.Map.Name, MapView.Active.Map.Name);
  }

  public DocumentModelStore GetDocumentState() => _store;

  public void AddModel(ModelCard model) => _store.Models.Add(model);

  public void UpdateModel(ModelCard model) => _store.UpdateModel(model);

  public void RemoveModel(ModelCard model) => _store.RemoveModel(model);

  public void HighlightObjects(List<string> objectIds) => HighlightObjectsOnView(objectIds);

  public void HighlightModel(string modelCardId)
  {
    var model = _store.GetModelById(modelCardId);

    if (model is null)
    {
      return;
    }

    var objectIds = new List<string>();

    if (model is SenderModelCard senderModelCard)
    {
      objectIds = senderModelCard.SendFilter.NotNull().GetObjectIds();
    }

    if (model is ReceiverModelCard receiverModelCard)
    {
      objectIds = receiverModelCard.BakedObjectIds.NotNull();
    }

    if (objectIds is null)
    {
      return;
    }
    HighlightObjectsOnView(objectIds);
  }

  private async void HighlightObjectsOnView(List<string> objectIds)
  {
    MapView mapView = MapView.Active;

    await QueuedTask
      .Run(() =>
      {
        List<MapMember> mapMembers = GetMapMembers(objectIds, mapView);
        ClearSelectionInTOC();
        ClearSelection();
        SelectMapMembersInTOC(mapMembers);
        SelectMapMembers(mapMembers);
        mapView.ZoomToSelected();
      })
      .ConfigureAwait(false);
  }

  private List<MapMember> GetMapMembers(List<string> objectIds, MapView mapView)
  {
    List<MapMember> mapMembers = new();

    foreach (string objectId in objectIds)
    {
      MapMember mapMember = mapView.Map.FindLayer(objectId);
      if (mapMember is null)
      {
        mapMember = mapView.Map.FindStandaloneTable(objectId);
      }
      if (mapMember is null)
      {
        continue;
      }
      mapMembers.Add(mapMember);
    }

    return mapMembers;
  }

  private void ClearSelection()
  {
    List<Layer> mapMembers = MapView.Active.Map.GetLayersAsFlattenedList().ToList();
    foreach (var member in mapMembers)
    {
      if (member is FeatureLayer featureLayer)
      {
        featureLayer.ClearSelection();
      }
    }
  }

  private void ClearSelectionInTOC()
  {
    MapView.Active.ClearTOCSelection();
  }

  private void SelectMapMembers(List<MapMember> mapMembers)
  {
    foreach (var member in mapMembers)
    {
      if (member is FeatureLayer layer)
      {
        layer.Select();
      }
    }
  }

  private void SelectMapMembersInTOC(List<MapMember> mapMembers)
  {
    List<Layer> layers = new();
    List<StandaloneTable> tables = new();

    foreach (MapMember member in mapMembers)
    {
      if (member is Layer layer)
      {
        layers.Add(layer);
      }
      else if (member is StandaloneTable table)
      {
        tables.Add(table);
      }
    }
    MapView.Active.SelectLayers(layers);
    // MapView.Active.SelectStandaloneTables(tables); // clears previous selection, not clear how to ADD selection instead
  }
}
