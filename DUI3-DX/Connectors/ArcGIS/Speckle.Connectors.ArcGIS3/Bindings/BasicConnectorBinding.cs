using System.Reflection;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
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

  public async void HighlightModel(string modelCardId)
  {
    MapView mapView = MapView.Active.NotNull(); // Not sure here we should throw?

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
      objectIds = receiverModelCard.ReceiveResult?.BakedObjectIds.NotNull();
    }

    if (objectIds is null)
    {
      return;
    }

    await QueuedTask
      .Run(() =>
      {
        List<MapMember> mapMembers = GetMapMembers(objectIds, mapView);
        ClearSelection();
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

  private void SelectMapMembers(List<MapMember> mapMembers)
  {
    foreach (var member in mapMembers)
    {
      if (member is FeatureLayer featureLayer)
      {
        using (RowCursor rowCursor = featureLayer.Search())
        {
          while (rowCursor.MoveNext())
          {
            using (var row = rowCursor.Current)
            {
              if (row is not Feature feature)
              {
                continue;
              }
              Geometry geometry = feature.GetShape();
              MapView.Active.SelectFeatures(geometry, SelectionCombinationMethod.Add);
            }
          }
        }
      }
    }
  }
}
