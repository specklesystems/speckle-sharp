using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class TestCommand : IExternalCommand
  {
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;

    internal static UIApplication uiapp;

    public static ConnectorBindingsRevit2 Bindings { get; set; }


    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      uiapp = commandData.Application;
      try
      {
        var x = Bindings.PreviewReceive(null, null).Result;
      }
      catch (Exception e)
      {

      }


      return Result.Succeeded;
    }
  }

}
