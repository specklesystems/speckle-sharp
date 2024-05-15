using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Connectors.ArcGIS.Bindings;

public class ArcGISSelectionBinding : ISelectionBinding
{
  private const string SELECTION_EVENT = "setSelection";

  public string Name { get; } = "selectionBinding";
  public IBridge Parent { get; set; }

  public ArcGISSelectionBinding(IBridge parent)
  {
    Parent = parent;

    // example: https://github.com/Esri/arcgis-pro-sdk-community-samples/blob/master/Map-Authoring/QueryBuilderControl/DefinitionQueryDockPaneViewModel.cs
    MapViewEventArgs args = new(MapView.Active);
    TOCSelectionChangedEvent.Subscribe(OnSelectionChanged, true);
  }

  private void OnSelectionChanged(MapViewEventArgs args)
  {
    SelectionInfo selInfo = GetSelection();
    Parent?.Send(SELECTION_EVENT, selInfo);
  }

  public SelectionInfo GetSelection()
  {
    MapView mapView = MapView.Active;
    List<MapMember> selectedMembers = new();
    selectedMembers.AddRange(mapView.GetSelectedLayers());
    selectedMembers.AddRange(mapView.GetSelectedStandaloneTables());

    List<string> objectTypes = selectedMembers
      .Select(o => o.GetType().ToString().Split(".").Last())
      .Distinct()
      .ToList();
    return new SelectionInfo(
      selectedMembers.Select(x => x.URI).ToList(),
      $"{selectedMembers.Count} layers ({string.Join(", ", objectTypes)})"
    );
  }
}
