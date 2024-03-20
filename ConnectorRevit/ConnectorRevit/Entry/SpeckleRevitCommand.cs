using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    ConflictCheck(commandData.Application);

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
    created = DockablePane.PaneExists(PanelId);

    //if revit was launched double-clicking on a Revit file, we're screwed
    //could maybe show the old window?
    if (!created && App.AppInstance.Application.Documents.Size > 0)
    {
      TaskDialog mainDialog = new("Dockable Panel Issue");
      mainDialog.MainInstruction = "Dockable Panel Issue";
      mainDialog.MainContent =
        "Revit cannot properly register Dockable Panels when launched by double-clicking a Revit file. "
        + "Please close and re-open Revit without launching a file OR open/create a new project to trigger the Speckle panel registration.";

      // Set footer text. Footer text is usually used to link to the help document.
      mainDialog.FooterText =
        "<a href=\"https://github.com/specklesystems/speckle-sharp/issues/1469 \">" + "Click here for more info</a>";

      mainDialog.Show();
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

  private static void ConflictCheck(UIApplication revituiapp)
  {
    // Get loaded plugins in Revit
    var loadedApps = revituiapp.LoadedApplications.Cast<IExternalApplication>().ToList();
    var speckle = loadedApps.FirstOrDefault(x => x.ToString().Contains("Speckle.ConnectorRevit"));
    var speckleReferences = speckle.GetType().Assembly.GetReferencedAssemblies().ToList();

    // Get Core's references
    // NOTE: Connector needs to be initialized otherwise Core will not be loaded in the AppDomain yet
    // NOTE2: maybe we should iteratively loop all downstream references to be 100% sure?
    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName).ToList();
    var core = loadedAssemblies.FirstOrDefault(x => x.FullName.Contains("SpeckleCore2"));
    var coreReferences = core.GetReferencedAssemblies().ToList();
    speckleReferences.AddRange(coreReferences);

    loadedApps.Remove(speckle);

    var output = "";

    var t = new Stopwatch();
    t.Start();

    foreach (var app in loadedApps)
    {
      var appReferences = app.GetType().Assembly.GetReferencedAssemblies().ToList();

      foreach (var appReference in appReferences)
      {
        var match = speckleReferences.FirstOrDefault(x => x.Name == appReference.Name);
        if (match == null || match.Version.Equals(appReference.Version))
          continue;
        else
          output += $"Conflict found: {app.ToString()}, {appReference.Name} v{appReference.Version} - expected v{match.Version}\n";

      }
    }
    t.Stop();

    output += $"\nCheck completed in {t.Elapsed.TotalSeconds}s";

    TaskDialog.Show("Conflict Report 🔥", output);
  }

  private static void AppMain(Avalonia.Application app, string[] args)
  {
    AvaloniaApp = app;
  }
}
