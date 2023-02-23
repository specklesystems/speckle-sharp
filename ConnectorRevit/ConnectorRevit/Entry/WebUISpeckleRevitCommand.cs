using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  /// <summary>
  /// This is the ExternalCommand which gets executed from the ExternalApplication. In a WPF context,
  /// this can be lean, as it just needs to show the WPF. Without a UI, this could contain the main
  /// order of operations for executing the business logic.
  /// </summary>
  [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
  public class WebUISpeckleRevitCommand : IExternalCommand
  {
    private static Speckle.ConnectorRevit.WebUIPanel _panel { get; set; }

    internal static DockablePaneId PanelId = new DockablePaneId(new Guid("{A3D4C26B-238A-440A-ACA3-79F0B8862AB7}"));

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      try
      {
        RegisterPane();
        var panel = App.AppInstance.GetDockablePane(PanelId);
        panel.Show();
      }
      catch (Exception ex)
      {
        Serilog.Log.Error(ex, ex.Message);
      }

      return Result.Succeeded;
    }

    internal static void RegisterPane()
    {
      try
      {
        var registered = DockablePane.PaneIsRegistered(PanelId);
        var created = DockablePane.PaneExists(PanelId);

        if (registered && created)
        {
          return;
        }

        if (!registered)
        {
          //Register dockable panel
          _panel = new Speckle.ConnectorRevit.WebUIPanel(new RevitWebUIBindings());
          App.AppInstance.RegisterDockablePane(PanelId, "Speckle", _panel);
        }
        created = DockablePane.PaneExists(PanelId);

        //if revit was launched double-clicking on a Revit file, we're screwed
        //could maybe show the old window?
        if (!created && App.AppInstance.Application.Documents.Size > 0)
        {
          TaskDialog mainDialog = new TaskDialog("Dockable Panel Issue");
          mainDialog.MainInstruction = "Dockable Panel Issue";
          mainDialog.MainContent =
              "Revit cannot properly register Dockable Panels when launched by double-clicking a Revit file. "
              + "Please close and re-open Revit without launching a file OR open/create a new project to trigger the Speckle panel registration.";

          // Set footer text. Footer text is usually used to link to the help document.
          mainDialog.FooterText =
              "<a href=\"https://github.com/specklesystems/speckle-sharp/issues/1469 \">"
              + "Click here for more info</a>";

          mainDialog.Show();
        }
      }
      catch (Exception ex)
      {
        Serilog.Log.Error(ex, ex.Message);
        var td = new TaskDialog("Error");
        td.MainContent = $"Oh no! Something went wrong while loading Speckle, please report it on the forum:\n{ex.Message}";
        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Report issue on our Community Forum");

        TaskDialogResult tResult = td.Show();

        if (TaskDialogResult.CommandLink1 == tResult)
        {
          Process.Start("https://speckle.community/");
        }
      }
    }
  }
}
