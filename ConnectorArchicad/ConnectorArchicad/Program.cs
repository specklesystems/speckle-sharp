using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;

namespace Archicad.Launcher
{
  class Program
  {
    public static Window? MainWindow { get; private set; }
    public static ArchicadBinding? Bindings { get; set; }

    public static void Main(string[] args)
    {
      if (args.Length == 0)
      {
        System.Diagnostics.Debug.Fail("Communication port number is missing!");
        return;
      }

      if (!uint.TryParse(args.First(), out uint portNumber))
      {
        System.Diagnostics.Debug.Fail("Invalid communication port number!");
        return;
      }

      Communication.ConnectionManager.Instance.Start(portNumber);

      Bindings = new ArchicadBinding();
      CreateOrFocusSpeckle(args);
      // BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
    }

    public static void CreateOrFocusSpeckle(string[] args)
    {
      if (MainWindow == null)
        BuildAvaloniaApp().Start(AppMain, args);

      MainWindow.Show();
      MainWindow.Activate();
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new X11PlatformOptions { UseGpu = false })
      .With(new MacOSPlatformOptions { ShowInDock = true, DisableDefaultApplicationMenuItems = true, DisableNativeMenus = true })
      .With(new AvaloniaNativePlatformOptions { UseGpu = false, UseDeferredRendering = true })
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();

    private static void AppMain(Application app, string[] args)
    {
      var viewModel = new MainViewModel(Bindings);
      MainWindow = new MainWindow { DataContext = viewModel };

      app.Run(MainWindow);
    }
  }
}
