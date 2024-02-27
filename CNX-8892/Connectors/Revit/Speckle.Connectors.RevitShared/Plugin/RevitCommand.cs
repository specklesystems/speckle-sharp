using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.Connectors.Revit;
using Speckle.Connectors.Revit.Plugin;

namespace Speckle.Connectors.Revit.Plugin;

[Transaction(TransactionMode.Manual)]
internal class SpeckleRevitCommand : IExternalCommand
{
  // POC: for devtools
  public static CefSharpPanel Panel;

  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    DockablePane panel = commandData.Application.GetDockablePane(RevitExternalApplication.DoackablePanelId);
    panel.Show();

    Panel.ShowDevTools();
    return Result.Succeeded;
  }
}
