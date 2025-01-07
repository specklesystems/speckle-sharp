using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorAutocadCivil.UI;
using Speckle.Core.Logging;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Exception = System.Exception;
#if ADVANCESTEEL
using Autodesk.AdvanceSteel.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif

#if ADVANCESTEEL
[assembly: CommandClass(typeof(Speckle.ConnectorAutocadCivil.Entry.SpeckleAutocadCommand))]

#endif

namespace Speckle.ConnectorAutocadCivil.Entry;

public class SpeckleAutocadCommand
{
  #region Avalonia parent window
  [DllImport("user32.dll", SetLastError = true)]
  [SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes")]
  static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);

  const int GWL_HWNDPARENT = -8;
  #endregion
  private static Avalonia.Application AvaloniaApp { get; set; }
  public static Window MainWindow { get; private set; }
  private static CancellationTokenSource Lifetime = null;
  public static ConnectorBindingsAutocad Bindings { get; set; }

  public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder
      .Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();

  /// <summary>
  /// Main command to initialize Speckle Connector
  /// </summary>
  [CommandMethod("Speckle", CommandFlags.Modal)]
  public static void SpeckleCommand()
  {
    CreateOrFocusSpeckle();
  }

  public static void InitAvalonia()
  {
    BuildAvaloniaApp().Start(AppMain, null);
  }

  [SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Is top level plugin catch"
  )]
  public static void CreateOrFocusSpeckle(bool showWindow = true)
  {
    if (MainWindow == null)
    {
      MainViewModel viewModel = new(Bindings);
      MainWindow = new MainWindow { DataContext = viewModel };
    }

    try
    {
      if (showWindow)
      {
        MainWindow.Show();
        MainWindow.Activate();

        //required to gracefully quit avalonia and the skia processes
        //https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes
        if (Lifetime == null)
        {
          Lifetime = new CancellationTokenSource();
          Task.Run(() => AvaloniaApp.Run(Lifetime.Token));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          var parentHwnd = Application.MainWindow.Handle;
          var hwnd = MainWindow.PlatformImpl.Handle.Handle;
          SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
        }
      }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to create or focus Speckle window");
    }
  }

  private static void AppMain(Avalonia.Application app, string[] args)
  {
    AvaloniaApp = app;
  }

  [CommandMethod("SpeckleCommunity", CommandFlags.ActionMacro)]
  public static void SpeckleCommunity()
  {
    try
    {
      Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
        "_browser https://speckle.community ",
        false,
        false,
        true
      );
    }
    catch (System.Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Could not execute opening browser link for Speckle Community: {exceptionMessage}");
    }
  }

  [CommandMethod("SpeckleTutorials", CommandFlags.ActionMacro)]
  public static void SpeckleTutorials()
  {
    try
    {
      Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
        "_browser https://speckle.systems/tutorials ",
        false,
        false,
        true
      );
    }
    catch (System.Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Could not execute opening browser link for Speckle Tutorials: {exceptionMessage}");
    }
  }

  [CommandMethod("SpeckleDocs", CommandFlags.ActionMacro)]
  public static void SpeckleDocs()
  {
    try
    {
      Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
        "_browser https://speckle.guide/user/autocadcivil.html ",
        false,
        false,
        true
      );
    }
    catch (System.Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Could not execute opening browser link for Speckle Docs: {exceptionMessage}");
    }
  }
}
