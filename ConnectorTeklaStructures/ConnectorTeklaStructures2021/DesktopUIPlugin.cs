using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using System.Threading.Tasks;

namespace Speckle.ConnectorTeklaStructures
{
  public class DesktopUIPlugin
  {
    public static Window MainWindow { get; private set; }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
.UsePlatformDetect()
.With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
.With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
.LogToTrace()
.UseReactiveUI();

    private static void AppMain(Application app, string[] args)
    {
      var viewModel = new MainWindowViewModel();
      MainWindow = new DesktopUI2.Views.MainWindow
      {
        DataContext = viewModel
      };

      //app.Run(MainWindow);
      Task.Run(() => app.Run(MainWindow));
    }
    public void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
      {
        BuildAvaloniaApp().Start(AppMain, null);
      }


      MainWindow.Show();
      MainWindow.Activate();
    }
  }
}
