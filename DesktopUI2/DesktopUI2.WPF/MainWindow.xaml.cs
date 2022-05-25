using Avalonia;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
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
      BuildAvaloniaApp().SetupWithoutStarting();
      InitializeComponent();
      var viewModel = new MainViewModel();
      this.DataContext = viewModel;

    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
    .UsePlatformDetect()
    .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
    .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
    .LogToTrace()
    .UseReactiveUI();
  }
}
