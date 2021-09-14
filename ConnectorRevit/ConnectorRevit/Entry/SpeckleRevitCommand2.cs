using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Revit.Async;
using Speckle.ConnectorRevit.UI;
using Speckle.DesktopUI;
using Stylet.Xaml;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class SpeckleRevitCommand2 : IExternalCommand
  {

    public static Window MainWindow { get; private set; }
    private static Avalonia.Application AvaloniaApp { get; set; }
    UIApplication uiapp;

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


    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      //Always initialize RevitTask ahead of time within Revit API context
      RevitTask.Initialize();
      uiapp = commandData.Application;
      //UIDocument uidoc = uiapp.ActiveUIDocument;
      //Application app = uiapp.Application;
      //Document doc = uidoc.Document;

      CreateOrFocusSpeckle();

      return Result.Succeeded;
    }

    public static void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
      {
        var viewModel = new MainWindowViewModel(new ConnectorBindingsRevit2(uiapp));
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };

        AvaloniaApp.Run(MainWindow);
      }
      else
        MainWindow.Show();
    }

    private static void AppMain(Avalonia.Application app, string[] args)
    {
      AvaloniaApp = app;
    }


  }

}
