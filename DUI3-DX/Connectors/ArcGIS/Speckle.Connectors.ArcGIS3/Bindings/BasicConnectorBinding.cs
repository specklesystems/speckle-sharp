using System.Reflection;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Connectors.ArcGIS.Utils;
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

  public void HighlightObjects(List<string> objectIds) =>
    HighlightObjectsOnView(objectIds.Select(x => new ObjectID(x)).ToList());

  public void HighlightModel(string modelCardId)
  {
    var model = _store.GetModelById(modelCardId);

    if (model is null)
    {
      return;
    }

    var objectIds = new List<ObjectID>();

    if (model is SenderModelCard senderModelCard)
    {
      objectIds = senderModelCard.SendFilter.NotNull().GetObjectIds().Select(x => new ObjectID(x)).ToList();
    }

    if (model is ReceiverModelCard receiverModelCard)
    {
      objectIds = receiverModelCard.BakedObjectIds.NotNull().Select(x => new ObjectID(x)).ToList();
    }

    if (objectIds is null)
    {
      return;
    }
    HighlightObjectsOnView(objectIds);
  }

  private async void HighlightObjectsOnView(List<ObjectID> objectIds)
  {
    MapView mapView = MapView.Active;

    await QueuedTask
      .Run(() =>
      {
        List<MapMemberFeature> mapMembersFeatures = GetMapMembers(objectIds, mapView);
        ClearSelectionInTOC();
        ClearSelection();
        SelectMapMembersInTOC(mapMembersFeatures);
        SelectMapMembersAndFeatures(mapMembersFeatures);
        mapView.ZoomToSelected();
      })
      .ConfigureAwait(false);
  }

  private List<MapMemberFeature> GetMapMembers(List<ObjectID> objectIds, MapView mapView)
  {
    // find the layer on the map (from the objectID) and add the featureID is available
    List<MapMemberFeature> mapMembersFeatures = new();

    foreach (ObjectID objectId in objectIds)
    {
      MapMember mapMember = mapView.Map.FindLayer(objectId.MappedLayerURI, true);
      if (mapMember is null)
      {
        mapMember = mapView.Map.FindStandaloneTable(objectId.MappedLayerURI);
      }
      if (mapMember is not null)
      {
        MapMemberFeature mapMembersFeat = new(mapMember, objectId.FeatureId);
        mapMembersFeatures.Add(mapMembersFeat);
      }
    }
    return mapMembersFeatures;
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

  private void SelectMapMembersAndFeatures(List<MapMemberFeature> mapMembersFeatures)
  {
    foreach (MapMemberFeature mapMemberFeat in mapMembersFeatures)
    {
      MapMember member = mapMemberFeat.MapMember;
      if (member is FeatureLayer layer)
      {
        if (mapMemberFeat.FeatureId == null)
        {
          // select full layer if featureID not specified
          layer.Select();
        }
        else
        {
          // query features by ID
          var objectIDfield = layer.GetFeatureClass().GetDefinition().GetObjectIDField();

          // FeatureID range starts from 0, but auto-assigned IDs in the layer start from 1
          QueryFilter anotherQueryFilter = new() { WhereClause = $"{objectIDfield} = {mapMemberFeat.FeatureId + 1}" };
          using (Selection onlyOneSelection = layer.Select(anotherQueryFilter, SelectionCombinationMethod.New)) { }
        }
      }
    }
  }

  private void SelectMapMembersInTOC(List<MapMemberFeature> mapMembersFeatures)
  {
    List<Layer> layers = new();
    List<StandaloneTable> tables = new();

    foreach (MapMemberFeature mapMemberFeat in mapMembersFeatures)
    {
      MapMember member = mapMemberFeat.MapMember;
      if (member is Layer layer)
      {
        if (member is not GroupLayer) // group layer selection clears other layers selection
        {
          layers.Add(layer);
        }
      }
      else if (member is StandaloneTable table)
      {
        tables.Add(table);
      }
    }
    MapView.Active.SelectLayers(layers);

    // this step clears previous selection, not clear how to ADD selection instead
    // this is why, activating it only if no layers are selected
    if (layers.Count == 0)
    {
      MapView.Active.SelectStandaloneTables(tables);
    }
  }
}
