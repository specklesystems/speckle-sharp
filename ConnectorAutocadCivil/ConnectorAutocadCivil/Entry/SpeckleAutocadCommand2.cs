using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
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
  public class SpeckleAutocadCommand2
  {
    #region Avalonia parent window
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
    #endregion

    public static Window MainWindow { get; private set; }

    public static ConnectorBindingsAutocad2 Bindings { get; set; }

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

    public static void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
        BuildAvaloniaApp().Start(AppMain, null);

      MainWindow.Show();
      MainWindow.Activate();

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        var parentHwnd = Application.MainWindow.Handle;
        var hwnd = MainWindow.PlatformImpl.Handle.Handle;
        SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
      }
    }

    private static void AppMain(Avalonia.Application app, string[] args)
    {
      var viewModel = new MainWindowViewModel(Bindings);
      MainWindow = new MainWindow
      {
        DataContext = viewModel
      };

      Task.Run(() => app.Run(MainWindow));
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

