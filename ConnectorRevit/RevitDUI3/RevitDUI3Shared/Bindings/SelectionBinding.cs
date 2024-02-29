using System.Linq;
using Autodesk.Revit.UI;
using DUI3;
using DUI3.Bindings;
using Speckle.ConnectorRevitDUI3.Utils;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SelectionBinding : ISelectionBinding
{
  public string Name { get; set; } = "selectionBinding";
  public IBridge Parent { get; set; }
  private static UIApplication RevitApp { get; set; }

  public SelectionBinding()
  {
    RevitApp = RevitAppProvider.RevitApp;

    // TODO: Need to figure it out equivalent of SelectionChanged for Revit2020
#if REVIT2023
    RevitApp.SelectionChanged += (_,_) => RevitIdleManager.SubscribeToIdle(OnSelectionChanged);
#endif

    RevitApp.ViewActivated += (_, _) =>
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
    // TODO: filter supported stuff
    var selectionIds = RevitApp.ActiveUIDocument.Selection.GetElementIds().Select(id => id.ToString()).ToList()
      ;
    return new SelectionInfo()
    {
      SelectedObjectIds = selectionIds,
      Summary = $"{selectionIds.Count} objects selected."
    };
  }
}
