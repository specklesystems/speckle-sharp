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
    //static object consoleLock = new object();
    //static ManualResetEvent finished = new ManualResetEvent(false);
    //static Result result = Result.Succeeded;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements)
    {
      UIApplication uiapp = commandData.Application;

      // create application instance (Revit doesn't have one already)
      if ( Application.Current == null ) new Application();

      // refocuses window if it already exists
      var window = Application.Current?.MainWindow;
      if (window != null)
      {
        window.Show();
        window.Focus();
        return Result.Succeeded;
      }

      // create a new Speckle Revit bindings instance
      var revitBindings = new ConnectorBindingsRevit(uiapp);

      // create an external event handler to raise actions
      var eventHandler = ExternalEvent.Create(new SpeckleExternalEventHandler(revitBindings));
      // Give it to our bindings so we can actually do stuff with revit
      revitBindings.SetExecutorAndInit(eventHandler);

      var bootstrapper = new Bootstrapper()
      {
        Bindings = revitBindings
      };
      bootstrapper.Setup(Application.Current);

      return Result.Succeeded;
    }
  }

}
