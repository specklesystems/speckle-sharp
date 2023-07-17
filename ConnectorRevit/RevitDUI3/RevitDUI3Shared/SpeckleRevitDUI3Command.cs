using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Speckle.ConnectorRevitDUI3;

[Transaction(TransactionMode.Manual)]
public class SpeckleRevitDUI3Command: IExternalCommand
{
  
  internal static DockablePaneId PanelId = new DockablePaneId(new Guid("{85F73DA4-3EF4-4870-BDBC-FD2D238EED31}"));
  
  private static Panel Panel { get; set; }
  
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    // TODO
    
    return Result.Succeeded;
  }

  internal static void RegisterPane()
  {
    var registered = DockablePane.PaneIsRegistered(PanelId);
    var created = DockablePane.PaneExists(PanelId);

    if (registered && created)
    {
      Panel.Init();
    }

    if (!registered)
    {
      // TODO: bindings and co. 
      Panel = new Panel();
      App.AppInstance.RegisterDockablePane(PanelId, "Speckle", Panel);
    }

    created = DockablePane.PaneExists(PanelId);
  }
}
