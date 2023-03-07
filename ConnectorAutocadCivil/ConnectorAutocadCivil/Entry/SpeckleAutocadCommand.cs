using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

using Autodesk.AutoCAD.ApplicationServices;

#if ADVANCESTEEL2023
using Autodesk.AdvanceSteel.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif

using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

using DesktopUI2;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorAutocadCivil.UI;

namespace Speckle.ConnectorAutocadCivil.Entry
{
  public class SpeckleAutocadCommand
  {
    #region Avalonia parent window
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
    #endregion
    private static Avalonia.Application AvaloniaApp { get; set; }
    public static Window MainWindow { get; private set; }
    private static CancellationTokenSource Lifetime = null;
    public static ConnectorBindingsAutocad Bindings { get; set; }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
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

    public static void CreateOrFocusSpeckle(bool showWindow = true)
    {
      if (MainWindow == null)
      {
        var viewModel = new MainViewModel(Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };
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
      catch { }
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
        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_browser https://speckle.community ", false, false, true);
      }
      catch { }
    }

    [CommandMethod("SpeckleTutorials", CommandFlags.ActionMacro)]
    public static void SpeckleTutorials()
    {
      try
      {
        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_browser https://speckle.systems/tutorials ", false, false, true);
      }
      catch { }
    }

    [CommandMethod("SpeckleDocs", CommandFlags.ActionMacro)]
    public static void SpeckleDocs()
    {
      try
      {
        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_browser https://speckle.guide/user/autocadcivil.html ", false, false, true);
      }
      catch { }
    }
  }

  /*
  [CommandMethod("SpeckleSchema", CommandFlags.UsePickSet | CommandFlags.Transparent)]
  public static void SetSchema()
  {
    var ids = new List<ObjectId>();
    PromptSelectionResult selection = Doc.Editor.GetSelection();
    if (selection.Status == PromptStatus.OK)
      ids = selection.Value.GetObjectIds().ToList();
    foreach (var id in ids)
    {
      // decide schema here, assumption or user input.
      string schema = "";
      switch (id.ObjectClass.DxfName)
      {
        case "LINE":
          schema = "Column";
          break;
      }

      // add schema to object XData
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        DBObject obj = tr.GetObject(id, OpenMode.ForWrite);
        if (obj.XData == null)
          obj.XData = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text), schema));
        else
          obj.XData.Add(new TypedValue(Convert.ToInt32(DxfCode.Text), schema));
      }
    }
  }
  */
}

