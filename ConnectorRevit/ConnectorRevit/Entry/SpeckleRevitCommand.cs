using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.ConnectorRevit.UI;
using Speckle.DesktopUI;
using Stylet.Xaml;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class SpeckleRevitCommand : IExternalCommand
  {
    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsRevit Bindings { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      OpenOrFocusSpeckle();
      return Result.Succeeded;
    }

    public static void OpenOrFocusSpeckle()
    {
      try
      {
        if ( Bootstrapper != null )
        {
          Bootstrapper.ShowRootView();
          return;
        }

        Bootstrapper = new Bootstrapper() {Bindings = Bindings};

        new ApplicationLoader() {Bootstrapper = Bootstrapper};

        Bootstrapper.Start(null);
      }
      catch ( Exception e )
      {
        Bootstrapper = null;
      }
    }
  }
}
