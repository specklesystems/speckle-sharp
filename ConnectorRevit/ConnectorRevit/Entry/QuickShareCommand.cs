using System;
using System.Runtime.InteropServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DesktopUI2.ViewModels.Share;
using DesktopUI2.Views;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class QuickShareCommand : IExternalCommand
  {
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;

    internal static UIApplication uiapp;

    public static ConnectorBindingsRevit2 Bindings { get; set; }


    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      uiapp = commandData.Application;
      CreateOrFocusSpeckle();

      return Result.Succeeded;
    }

    public static void CreateOrFocusSpeckle()
    {

      var scheduler = new Share
      {
        DataContext = new ShareViewModel(Bindings)
      };

      scheduler.Show();

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        var parentHwnd = uiapp.MainWindowHandle;
        var hwnd = scheduler.PlatformImpl.Handle.Handle;
        SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
      }

    }
  }

}
