using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Speckle.ConnectorAutocadCivil.UI;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Speckle.ConnectorAutocadCivil.Entry
{
  public class SpeckleAutocadCommand2
  {
    public static Window MainWindow { get; private set; }
    public static ConnectorBindingsAutocad2 Bindings { get; set; }
    private static Avalonia.Application AvaloniaApp { get; set; }

    public static async void InitAvalonia()
    {
      BuildAvaloniaApp().Start(AppMain, null);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();

    /// <summary>
    /// Main command to initialize Speckle Connector
    /// </summary>
    [CommandMethod("Speckle2", CommandFlags.Modal)]
    public static void SpeckleCommand()
    {
      CreateOrFocusSpeckle();
    }

    public static void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
      {
        BuildAvaloniaApp().Start(AppMain, null);

        /*
        var viewModel = new MainWindowViewModel(Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel,
        };
        Task.Run(() => AvaloniaApp.Run(MainWindow));
        */
      }
      MainWindow.Show();
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

