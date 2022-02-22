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
  public class SpeckleCommand2 : Command
  {
    #region Avalonia parent window
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
    #endregion

    public static SpeckleCommand2 Instance { get; private set; }

    public override string EnglishName => "Speckle";

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
        BuildAvaloniaApp().Start(AppMain, null);

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
      var viewModel = new MainWindowViewModel(Bindings);
      MainWindow = new MainWindow
      {
        DataContext = viewModel
      };

      Task.Run(() => app.Run(MainWindow));
    }
  }
}
