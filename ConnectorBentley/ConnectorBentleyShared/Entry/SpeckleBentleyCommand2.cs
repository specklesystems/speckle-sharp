using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;

using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

using Speckle.ConnectorBentley.UI;
using DesktopUI2;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Stylet.Xaml;

namespace Speckle.ConnectorBentley.Entry
{
  public class SpeckleBentleyCommand2
  {
    public static Window MainWindow { get; private set; }
    public static ConnectorBindingsBentley2 Bindings { get; set; }
    private static Avalonia.Application AvaloniaApp { get; set; }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();

    public static void InitAvalonia()
    {
      try
      {
        BuildAvaloniaApp().Start(AppMain, null);
      }
      catch (Exception e)
      {

      }
    }

    public static void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
      {
        var viewModel = new MainWindowViewModel(Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };
        Task.Run(() => AvaloniaApp.Run(MainWindow));
      }
      MainWindow.Show();
      MainWindow.Activate();
    }

    private static void AppMain(Avalonia.Application app, string[] args)
    {
      AvaloniaApp = app;
    }

  }
}
