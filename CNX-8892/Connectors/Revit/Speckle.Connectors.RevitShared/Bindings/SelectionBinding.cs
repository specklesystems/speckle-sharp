using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.Plugin;

namespace Speckle.ConnectorRevitDUI3.Bindings;

internal class SelectionBinding : ISelectionBinding
{
  public string Name { get; set; } = "selectionBinding";

  public IBridge Parent { get; private set; }

  private readonly UIApplication _uiApplication;

  public SelectionBinding(IRevitPlugin revitPlugin)
  {
    _uiApplication = revitPlugin.UIApplication;

    // TODO: Need to figure it out equivalent of SelectionChanged for Revit2020
#if REVIT2023
    _uiApplication.SelectionChanged += (_,_) => RevitIdleManager.SubscribeToIdle(OnSelectionChanged);
#endif

    _uiApplication.ViewActivated += (_, _) =>
    {
      Parent?.SendToBrowser(SelectionBindingEvents.SetSelection, new SelectionInfo());
    };
  }

  private void OnSelectionChanged()
  {
    var selectionInfo = GetSelection();
    Parent?.SendToBrowser(SelectionBindingEvents.SetSelection, selectionInfo);
  }

  public SelectionInfo GetSelection()
  {
    List<Element> els = _uiApplication.ActiveUIDocument.Selection
      .GetElementIds()
      .Select(id => _uiApplication.ActiveUIDocument.Document.GetElement(id))
      .ToList();
    List<string> cats = els.Select(el => el.Category?.Name ?? el.Name).Distinct().ToList();
    List<string> ids = els.Select(el => el.UniqueId.ToString()).ToList();
    return new SelectionInfo()
    {
      SelectedObjectIds = ids,
      Summary = $"{els.Count} objects ({string.Join(", ", cats)})"
    };
  }
}
