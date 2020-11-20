using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.ConnectorRevit.UI;
using Speckle.DesktopUI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class Cmd : IExternalCommand
  {

    public static Bootstrapper Bootstrapper { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      if (Bootstrapper != null)
      {
        Bootstrapper.Application.MainWindow.Show();
        return Result.Succeeded;
      }

      UIApplication uiapp = commandData.Application;

      var bindings = new ConnectorBindingsRevit(uiapp);
      var eventHandler = ExternalEvent.Create(new SpeckleExternalEventHandler(bindings));
      bindings.SetExecutorAndInit(eventHandler);

      Bootstrapper = new Bootstrapper()
      {
        Bindings = bindings
      };

      Bootstrapper.Setup(Application.Current != null ? Application.Current : new Application());

      Bootstrapper.Application.Startup += (o, e) =>
      {
        var helper = new System.Windows.Interop.WindowInteropHelper(Bootstrapper.Application.MainWindow);
        helper.Owner = commandData.Application.MainWindowHandle;
      };

      commandData.Application.Application.DocumentOpened += FirstDocOpen;

      return Result.Succeeded;
    }

    private void FirstDocOpen(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
    {
      
    }
  }

}
