using System;
using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using System.Threading.Tasks;

namespace SpeckleRhino
{

  public class SpeckleCommand2 : Command
  {
    public static SpeckleCommand2 Instance { get; private set; }

    public override string EnglishName => "SpeckleNewUi";

    public static Window MainWindow { get; private set; }
    public static ConnectorBindingsRhino2 Bindings { get; set; } = new ConnectorBindingsRhino2();

    public SpeckleCommand2()
    {
      Instance = this;
    }


    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      CreateOrFocusSpeckle();
      return Result.Success;
    }

    public static void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
      {

        BuildAvaloniaApp().Start(AppMain, null);

      }

      MainWindow.Show();
    }

    private static void AppMain(Application app, string[] args)
    {
      var viewModel = new MainWindowViewModel(Bindings);
      MainWindow = new MainWindow
      {
        DataContext = viewModel
      };

      Task.Run(() => app.Run(MainWindow));
    }
  }
}
