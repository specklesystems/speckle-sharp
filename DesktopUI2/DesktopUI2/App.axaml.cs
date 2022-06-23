using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;

namespace DesktopUI2
{
  public class App : Application
  {
    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
      this.Name = "Speckle";
    }

    public override void OnFrameworkInitializationCompleted()
    {
      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        desktop.MainWindow = new MainWindow
        {
          DataContext = new MainWindowViewModel(),
        };

        //desktop.MainWindow = new Scheduler
        //{
        //  DataContext = new SchedulerViewModel(),
        //};

        //desktop.MainWindow = new Share
        //{
        //  DataContext = new ShareViewModel(),
        //};
      }

      base.OnFrameworkInitializationCompleted();
    }
  }
}
