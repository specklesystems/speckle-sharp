using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.Connectors.Revit.Plugin;

[Transaction(TransactionMode.Manual)]
internal class SpeckleRevitDui3Command : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    DockablePane panel = commandData.Application.GetDockablePane(RevitExternalApplication.DoackablePanelId);
    panel.Show();

    // App.CefSharpPanel.Browser.ShowDevTools();
    return Result.Succeeded;
  }
}
