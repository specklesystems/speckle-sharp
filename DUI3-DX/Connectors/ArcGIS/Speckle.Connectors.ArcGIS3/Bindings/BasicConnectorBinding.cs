using System.Reflection;
using ArcGIS.Desktop.Core;
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

  public DocumentInfo GetDocumentInfo() => new(Project.Current.URI, Project.Current.Name, Project.Current.Name);

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
        Dictionary<string, (MapMember member, List<int>? rows)> mapMembersRows = GetMapMembers(objectIds, mapView);
        ClearSelectionInTOC();
        ClearSelection();
        SelectMapMembersInTOC(mapMembersRows.Values.ToList().Select(x => x.member).ToList());
        SelectMapMembers(mapMembersRows);
        mapView.ZoomToSelected();
      })
      .ConfigureAwait(false);
  }

  private Dictionary<string, (MapMember member, List<int>? rows)> GetMapMembers(List<string> objectIds, MapView mapView)
  {
    Dictionary<string, (MapMember member, List<int>? rows)> mapMembersRows = new();

    foreach (string objectId in objectIds)
    {
      List<string> uriRows = objectId.Split("_speckleRowIndex_").ToList();
      string mapURI = uriRows[0];
      int? index = null;
      if (uriRows.Count > 1)
      {
        index = int.Parse(uriRows[1]);
      }

      MapMember mapMember = mapView.Map.FindLayer(mapURI);
      if (mapMember is null)
      {
        mapMember = mapView.Map.FindStandaloneTable(mapURI);
      }
      if (mapMember is null)
      {
        continue;
      }
      else
      {
        mapMembersRows[mapMember.URI] = (mapMember, null);
      }

      // add rows if applicable
      if (index == null)
      {
        mapMembersRows[mapMember.URI] = (mapMember, null);
      }
      else
      {
        if (!mapMembersRows.TryGetValue(mapMember.URI, out _))
        {
          mapMembersRows[mapMember.URI] = (mapMember, new List<int>());
        }
        else
        {
          mapMembersRows[mapMember.URI].rows?.Add((int)index);
        }
      }
    }

    return mapMembersRows;
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

  private void SelectMapMembers(Dictionary<string, (MapMember member, List<int>? rows)> mapMemberRows)
  {
    foreach (var data in mapMemberRows)
    {
      if (data.Value.member is FeatureLayer layer)
      {
        if (data.Value.rows == null)
        {
          layer.Select();
        }
        else
        {
          // POC: select by rows instead
          layer.Select();
        }
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
