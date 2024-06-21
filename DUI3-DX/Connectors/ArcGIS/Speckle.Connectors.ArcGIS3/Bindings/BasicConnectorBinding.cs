using System.Reflection;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
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
        List<ObjectID> objectIdAndmapMembers = GetMapMembers(objectIds, mapView);
        ClearSelectionInTOC();
        ClearSelection();
        SelectMapMembersInTOC(objectIdAndmapMembers);
        SelectMapMembersAndFeatures(objectIdAndmapMembers);
        mapView.ZoomToSelected();
      })
      .ConfigureAwait(false);
  }

  private List<ObjectID> GetMapMembers(List<ObjectID> objectIds, MapView mapView)
  {
    List<ObjectID> objectIdAndmapMembers = new();

    foreach (ObjectID objectId in objectIds)
    {
      MapMember mapMember = mapView.Map.FindLayer(objectId.MappedLayerURI);
      if (mapMember is null)
      {
        mapMember = mapView.Map.FindStandaloneTable(objectId.MappedLayerURI);
      }
      if (mapMember is null)
      {
        continue;
      }

      ObjectID newObjectId = new(objectId.MappedLayerURI, objectId.FeatureId, mapMember);
      objectIdAndmapMembers.Add(newObjectId);
    }

    return objectIdAndmapMembers;
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

  private void SelectMapMembersAndFeatures(List<ObjectID> objectIdAndmapMembers)
  {
    foreach (ObjectID objectId in objectIdAndmapMembers)
    {
      if (objectId.MapMember == null)
      {
        continue;
      }

      MapMember member = objectId.MapMember;
      if (member is FeatureLayer layer)
      {
        // select full layer if featureID not specified
        if (objectId.FeatureId == null)
        {
          layer.Select();
        }
        else
        {
          using (RowCursor rowCursor = layer.Search())
          {
            int index = 0;
            while (rowCursor.MoveNext())
            {
              if (index == objectId.FeatureId)
              {
                //Get the shape from the row and set extent
                using (var feature = rowCursor.Current as Feature)
                {
                  Geometry? geometry = feature?.GetShape();
                  MapView.Active.SelectFeatures(geometry, SelectionCombinationMethod.Add);
                }
                break;
              }
              index += 1;
            }
          }
        }
      }
    }
  }

  private void SelectMapMembersInTOC(List<ObjectID> objectIdAndmapMembers)
  {
    List<Layer> layers = new();
    List<StandaloneTable> tables = new();

    foreach (ObjectID objectId in objectIdAndmapMembers)
    {
      if (objectId.MapMember == null)
      {
        continue;
      }

      MapMember member = objectId.MapMember;
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
