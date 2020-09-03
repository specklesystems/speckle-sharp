using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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

      var win = new MainWindow();
      win.Show();

      return Result.Succeeded;
    }
  }


}
