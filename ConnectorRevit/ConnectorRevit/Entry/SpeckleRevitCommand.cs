using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorRevit.UI;

namespace Speckle.ConnectorRevit.Entry;

[Transaction(TransactionMode.Manual)]
public class SpeckleRevitCommand : IExternalCommand
{
  public static bool UseDockablePanel = true;

  //window stuff
  [DllImport("user32.dll", SetLastError = true)]
  [SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes")]
  static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);

  const int GWL_HWNDPARENT = -8;
  public static Window MainWindow { get; private set; }
  private static Avalonia.Application AvaloniaApp { get; set; }

  //end window stuff

  public static ConnectorBindingsRevit Bindings { get; set; }

  private static Panel _panel { get; set; }

  internal static DockablePaneId PanelId = new(new Guid("{0A866FB8-8FD5-4DE8-B24B-56F4FA5B0836}"));

  public static void InitAvalonia()
  {
    BuildAvaloniaApp().SetupWithoutStarting();
  }

  public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder
      .Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();

  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    if (UseDockablePanel)
    {
      RegisterPane();
      var panel = App.AppInstance.GetDockablePane(PanelId);
      panel.Show();
    }
    else
    {
      CreateOrFocusSpeckle();
    }

    return Result.Succeeded;
  }

  internal static void RegisterPane()
  {
    if (!UseDockablePanel)
    {
      return;
    }

    var registered = DockablePane.PaneIsRegistered(PanelId);
    var created = DockablePane.PaneExists(PanelId);

    if (registered && created)
    {
      _panel.Init();
      return;
    }

    if (!registered)
    {
      //Register dockable panel
      var viewModel = new MainViewModel(Bindings);
      _panel = new Panel { DataContext = viewModel };
      App.AppInstance.RegisterDockablePane(PanelId, "Speckle", _panel);
      _panel.Init();
    }
  }

  public static void CreateOrFocusSpeckle(bool showWindow = true)
  {
    if (MainWindow == null)
    {
      var viewModel = new MainViewModel(Bindings);
      MainWindow = new MainWindow { DataContext = viewModel };

      //massive hack: we start the avalonia main loop and stop it immediately (since it's thread blocking)
      //to avoid an annoying error when closing revit
      var cts = new CancellationTokenSource();
      cts.CancelAfter(100);
      AvaloniaApp.Run(cts.Token);
    }

    if (showWindow)
    {
      MainWindow.Show();
      MainWindow.Activate();

      //required to gracefully quit avalonia and the skia processes
      //can also be used to manually do so
      //https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes


      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        var parentHwnd = App.AppInstance.MainWindowHandle;
        var hwnd = MainWindow.PlatformImpl.Handle.Handle;
        SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
      }
    }
  }

  private static void AppMain(Avalonia.Application app, string[] args)
  {
    AvaloniaApp = app;
  }
}
