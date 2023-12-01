using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CefSharp;

namespace Speckle.ConnectorRevitDUI3;

[Transaction(TransactionMode.Manual)]
public class SpeckleRevitDui3Command : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    DockablePane panel = commandData.Application.GetDockablePane(App.PanelId);
    panel.Show();

    App.CefSharpPanel.Browser.ShowDevTools();
    return Result.Succeeded;
  }
}
