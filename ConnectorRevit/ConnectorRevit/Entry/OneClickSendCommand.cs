using System;
using System.Runtime.InteropServices;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia.Controls;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class OneClickSendCommand : IExternalCommand
  {
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
    public static ConnectorBindingsRevit2 Bindings { get; set; }
    public static StreamState FileStream { get; set; }

    public static Window MainWindow { get; private set; }

    private static Avalonia.Application AvaloniaApp { get; set; }

    internal static UIApplication uiapp;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      uiapp = commandData.Application;

      // intialize dui2
      CreateOrFocusSpeckle(false);

      // send
      var oneClick = new OneClickViewModel(Bindings, FileStream);
      oneClick.Send();
      FileStream = oneClick.FileStream;

      return Result.Succeeded;
    }

    public static void CreateOrFocusSpeckle(bool showWindow = true)
    {


      if (MainWindow == null)
      {
        var viewModel = new MainViewModel(Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };

        //massive hack: we start the avalonia main loop and stop it immediately (since it's thread blocking)
        //to avoid an annoying error when closing revit
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);
        AvaloniaApp.Run(cts.Token);


      }

      try
      {
        if (showWindow)
        {
          MainWindow.Show();
          MainWindow.Activate();

          //required to gracefully quit avalonia and the skia processes
          //can also be used to manually do so
          //https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes


          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
          {
            var parentHwnd = uiapp.MainWindowHandle;
            var hwnd = MainWindow.PlatformImpl.Handle.Handle;
            SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
          }
        }
      }
      catch (Exception ex)
      {
      }
    }

    private static void AppMain(Avalonia.Application app, string[] args)
    {
      AvaloniaApp = app;
    }

  }

}
