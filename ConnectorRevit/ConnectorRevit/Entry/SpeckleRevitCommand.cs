using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.ConnectorRevit.UI;
using Speckle.DesktopUI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class SpeckleRevitCommand : IExternalCommand
  {

    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsRevit Bindings { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      OpenOrFocusSpeckle(commandData.Application);
      return Result.Succeeded;
    }

    public static void OpenOrFocusSpeckle(UIApplication app)
    {
      try
      {
        if (Bootstrapper != null)
        {
          Bootstrapper.Application.MainWindow.Show();
          return;
        }

        Bootstrapper = new Bootstrapper()
        {
          Bindings = Bindings
        };

        Bootstrapper.Setup(Application.Current != null ? Application.Current : new Application());

        Bootstrapper.Application.Startup += (o, e) =>
        {
          var helper = new System.Windows.Interop.WindowInteropHelper(Bootstrapper.Application.MainWindow);
          helper.Owner = app.MainWindowHandle;
        };

      }
      catch (Exception e)
      {

      }
    }

  }

}
