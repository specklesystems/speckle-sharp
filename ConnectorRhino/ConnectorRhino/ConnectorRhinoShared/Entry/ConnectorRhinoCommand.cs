using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;

using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

using DesktopUI2.ViewModels;
using DesktopUI2.Views;

namespace SpeckleRhino
{
  public class SpeckleCommand : Command
  {
    #region Avalonia parent window
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
    #endregion

    public static SpeckleCommand Instance { get; private set; }

    public override string EnglishName => "Speckle";

    public static Window MainWindow { get; private set; }

    public static ConnectorBindingsRhino Bindings { get; set; } = new ConnectorBindingsRhino();

    private static Avalonia.Application AvaloniaApp { get; set; }

    public SpeckleCommand()
    {
      Instance = this;
    }

    public static void InitAvalonia()
    {
      BuildAvaloniaApp().Start(AppMain, null);
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
        var viewModel = new MainWindowViewModel(Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };
      }

      MainWindow.Show();
      MainWindow.Activate();

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        var parentHwnd = RhinoApp.MainWindowHandle();
        var hwnd = MainWindow.PlatformImpl.Handle.Handle;
        SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
      }
    }

    private static void AppMain(Application app, string[] args)
    {
      AvaloniaApp = app;
    }
  }
}
