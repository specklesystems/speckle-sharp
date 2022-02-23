using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorRevit.UI;
using Stylet.Xaml;
using Application = Autodesk.Revit.ApplicationServices.Application;

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
    private static UIApplication uiapp;

    public static void InitAvalonia()
    {
      BuildAvaloniaApp().Start(AppMain, null);
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
      CreateOrFocusSpeckle();

      return Result.Succeeded;
    }

    private void MainWindow_StateChanged(object sender, EventArgs e)
    {
    }

    public static void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
      {
        var viewModel = new MainWindowViewModel(Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };

        Task.Run(() => AvaloniaApp.Run(MainWindow));
      }

      MainWindow.Show();
      MainWindow.Activate();

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        var parentHwnd = uiapp.MainWindowHandle;
        var hwnd = MainWindow.PlatformImpl.Handle.Handle;
        SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
      }
    }

    private static void AppMain(Avalonia.Application app, string[] args)
    {
      AvaloniaApp = app;
    }

  }

}
