using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Plugin;

namespace Speckle.Connectors.Revit.Bindings;

// POC: we need a base a RevitBaseBinding
internal class SelectionBinding : RevitBaseBinding, ISelectionBinding
{
  public SelectionBinding(
    RevitContext revitContext,
    RevitDocumentStore store,
    IBridge bridge,
    IBrowserSender browserSender
  )
    : base("selectionBinding", store, bridge, browserSender, revitContext)
  {
    // POC: we can inject the solution here
    // TODO: Need to figure it out equivalent of SelectionChanged for Revit2020
#if REVIT2023
    _uiApplication.SelectionChanged += (_,_) => RevitIdleManager.SubscribeToIdle(OnSelectionChanged);
#endif

    _revitContext.UIApplication.ViewActivated += (_, _) =>
    {
      _browserSender.Send(Bridge.FrontendBoundName, SelectionBindingEvents.SetSelection, new SelectionInfo());
    };
  }

  private void OnSelectionChanged()
  {
    _browserSender.Send(Bridge.FrontendBoundName, SelectionBindingEvents.SetSelection, GetSelection());
  }

  public SelectionInfo GetSelection()
  {
    List<Element> els = _revitContext.UIApplication.ActiveUIDocument.Selection
      .GetElementIds()
      .Select(id => _revitContext.UIApplication.ActiveUIDocument.Document.GetElement(id))
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
