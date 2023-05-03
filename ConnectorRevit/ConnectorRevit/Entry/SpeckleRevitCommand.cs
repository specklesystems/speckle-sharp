using System;
using System.Diagnostics;
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
using Speckle.Core.Logging;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class SpeckleRevitCommand : IExternalCommand
  {

    public static bool UseDockablePanel = true;

    //window stuff
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
    public static Window MainWindow { get; private set; }
    private static Avalonia.Application AvaloniaApp { get; set; }
    //end window stuff

    public static ConnectorBindingsRevit Bindings { get; set; }

    private static Panel _panel { get; set; }


    internal static DockablePaneId PanelId = new DockablePaneId(new Guid("{0A866FB8-8FD5-4DE8-B24B-56F4FA5B0836}"));


    public static void InitAvalonia()
    {
      BuildAvaloniaApp().SetupWithoutStarting();
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();



    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {

      if (UseDockablePanel)
      {
        try
        {
          RegisterPane();
          var panel = App.AppInstance.GetDockablePane(PanelId);
          panel.Show();
        }
        catch (Exception ex)
        {
          SpeckleLog.Logger.Error(ex, "Failed to show dockable panel");
        }
      }
      else
        CreateOrFocusSpeckle();



      return Result.Succeeded;
    }

    internal static void RegisterPane()
    {
      try
      {
        if (!UseDockablePanel)
          return;

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
          _panel = new Panel
          {
            DataContext = viewModel
          };
          App.AppInstance.RegisterDockablePane(PanelId, "Speckle", _panel);
          _panel.Init();
        }
        created = DockablePane.PaneExists(PanelId);

        //if revit was launched double-clicking on a Revit file, we're screwed
        //could maybe show the old window?
        if (!created && App.AppInstance.Application.Documents.Size > 0)
        {
          TaskDialog mainDialog = new TaskDialog("Dockable Panel Issue");
          mainDialog.MainInstruction = "Dockable Panel Issue";
          mainDialog.MainContent =
              "Revit cannot properly register Dockable Panels when launched by double-clicking a Revit file. "
              + "Please close and re-open Revit without launching a file OR open/create a new project to trigger the Speckle panel registration.";


          // Set footer text. Footer text is usually used to link to the help document.
          mainDialog.FooterText =
              "<a href=\"https://github.com/specklesystems/speckle-sharp/issues/1469 \">"
              + "Click here for more info</a>";

          mainDialog.Show();

        }
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Fatal(ex, "Failed to load Speckle command for host app");
        var td = new TaskDialog("Error");
        td.MainContent = $"Oh no! Something went wrong while loading Speckle, please report it on the forum:\n{ex.Message}";
        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Report issue on our Community Forum");

        TaskDialogResult tResult = td.Show();

        if (TaskDialogResult.CommandLink1 == tResult)
        {
          Process.Start("https://speckle.community/");
        }
      }

    }

    public static void CreateOrFocusSpeckle(bool showWindow = true)
    {
      try
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
      catch (Exception ex)
      {
        SpeckleLog.Logger.Fatal(ex, "Failed to create main window");
        var td = new TaskDialog("Error");
        td.MainContent = $"Oh no! Something went wrong while loading Speckle, please report it on the forum:\n{ex.Message}";
        td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Report issue on our Community Forum");

        TaskDialogResult tResult = td.Show();

        if (TaskDialogResult.CommandLink1 == tResult)
        {
          Process.Start("https://speckle.community/");
        }
      }
    }

    private static void AppMain(Avalonia.Application app, string[] args)
    {
      AvaloniaApp = app;
    }





  }

}
