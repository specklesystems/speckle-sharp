using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Rhino;
using Rhino.Commands;

namespace SpeckleRhino
{
  public class SpeckleCommand : Command
  {
#region Avalonia parent window
#if !MAC
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;
#endif
#endregion

    public static SpeckleCommand Instance { get; private set; }

    public override string EnglishName => "Speckle";

    public static Window MainWindow { get; private set; }


    private static CancellationTokenSource Lifetime = null;

    public static Avalonia.Application AvaloniaApp { get; set; }

    public SpeckleCommand()
    {
      Instance = this;
    }

    public static void InitAvalonia()
    {
      BuildAvaloniaApp().SetupWithoutStarting();
    }

    public static AppBuilder BuildAvaloniaApp()
    {
      return AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new X11PlatformOptions { UseGpu = false })
      .With(new AvaloniaNativePlatformOptions { UseGpu = false, UseDeferredRendering = true })
      .With(new MacOSPlatformOptions { ShowInDock = false, DisableDefaultApplicationMenuItems = true, DisableNativeMenus = true })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .LogToTrace()
      .UseReactiveUI();
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {

#if MAC
      var msg = "Speckle is temporarily disabled on Rhino due to a critical bug regarding Rhino's top-menu commands. Please use Grasshopper instead while we fix this.";
      RhinoApp.CommandLineOut.WriteLine(msg);
      Rhino.UI.Dialogs.ShowMessage(msg, "Speckle has been disabled", Rhino.UI.ShowMessageButton.OK, Rhino.UI.ShowMessageIcon.Exclamation);
      return Result.Nothing;
      //CreateOrFocusSpeckle();
#endif
      Rhino.UI.Panels.OpenPanel(typeof(Panel).GUID);

      return Result.Success;
    }

    public static void CreateOrFocusSpeckle()
    {
      SpeckleRhinoConnectorPlugin.Instance.Init();
      if (MainWindow == null)
      {
        var viewModel = new MainViewModel(SpeckleRhinoConnectorPlugin.Instance.Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };
      }

      MainWindow.Show();
      MainWindow.Activate();

#if !MAC
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        var parentHwnd = RhinoApp.MainWindowHandle();
        var hwnd = MainWindow.PlatformImpl.Handle.Handle;
        SetWindowLongPtr(hwnd, GWL_HWNDPARENT, parentHwnd);
      }
#endif
    }

    private static void AppMain(Application app, string[] args)
    {
      AvaloniaApp = app;
    }
  }
}
