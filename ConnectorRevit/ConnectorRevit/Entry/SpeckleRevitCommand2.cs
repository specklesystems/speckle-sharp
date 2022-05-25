using System;
using System.Runtime.InteropServices;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class SpeckleRevitCommand2 : IExternalCommand
  {
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
    public static Window MainWindow { get; private set; }
    public static ConnectorBindingsRevit2 Bindings { get; set; }
    private static Avalonia.Application AvaloniaApp { get; set; }
    internal static UIApplication uiapp;


    internal static DockablePaneId PanelId = new DockablePaneId(new Guid("{0A866FB8-8FD5-4DE8-B24B-56F4FA5B0836}"));


    public static void InitAvalonia()
    {
      BuildAvaloniaApp().SetupWithoutStarting();

      //AppBuilder.Configure<DesktopUI2.App>().UseWin32().UseDirect2D1().UseReactiveUI().SetupWithoutStarting();
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();



    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      uiapp = commandData.Application;

      DockablePane dp = commandData.Application.GetDockablePane(PanelId);
      dp.Show();

      //CreateOrFocusSpeckle();

      return Result.Succeeded;
    }

    void Application_ViewActivated(object sender, ViewActivatedEventArgs e)
    {
      //panel.lblProjectName.Content = e.Document.ProjectInformation.Name;
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
