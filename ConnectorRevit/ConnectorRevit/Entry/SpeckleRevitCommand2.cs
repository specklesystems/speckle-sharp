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

    private static Window MainWindow = null;
    public static ConnectorBindingsRevit2 Bindings { get; set; }
    UIApplication uiapp;

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
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;

      Document doc = uidoc.Document;
      try
      {
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

        if (MainWindow == null)
        {
          BuildAvaloniaApp().Start(AppMain, null);
        }
        else
          MainWindow.Show();

      }
      catch (Exception e)
      {

      }
      return Result.Succeeded;
    }

    void AppMain(Avalonia.Application app, string[] args)
    {
      var viewModel = new MainWindowViewModel(Bindings);
      MainWindow = new MainWindow
      {
        DataContext = viewModel
      };
      MainWindow.Show();

      Task.Run(() => app.Run(MainWindow));
    }

    static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly a = null;
      var name = args.Name.Split(',')[0];
      string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);

      return a;
    }
  }

}
