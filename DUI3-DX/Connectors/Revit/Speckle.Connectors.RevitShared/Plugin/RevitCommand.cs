using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Speckle.Connectors.Revit.Plugin;

[Transaction(TransactionMode.Manual)]
internal sealed class SpeckleRevitCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    DockablePane panel = commandData.Application.GetDockablePane(RevitExternalApplication.DoackablePanelId);
    panel.Show();

    return Result.Succeeded;
  }
}
