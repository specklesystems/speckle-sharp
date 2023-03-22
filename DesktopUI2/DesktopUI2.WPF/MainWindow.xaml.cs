using Avalonia;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.Core.Logging;
using System.Windows;

namespace DesktopUI2.WPF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      SpeckleLog.Initialize("dui", "2");
      BuildAvaloniaApp().SetupWithoutStarting();
      InitializeComponent();

      var viewModel = new MainViewModel();
      DataContext = viewModel;

      AvaloniaHost.Content = new MainUserControl();

    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
    .UsePlatformDetect()
    .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
    .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
    .LogToTrace()
    .UseReactiveUI();
  }
}
