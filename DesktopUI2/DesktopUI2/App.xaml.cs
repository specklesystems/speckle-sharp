using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Material.Styles.Themes;
using Speckle.Core.Kits;

namespace DesktopUI2;

public class App : Application
{
  //Speckle theme
  public static readonly Color Primary = Color.FromRgb(59, 130, 246);
  public static readonly Color Secondary = Color.FromRgb(131, 180, 255);
  public static readonly Color Accent = Color.FromRgb(255, 191, 0);

  public override void Initialize()
  {
    //NOTE: the Mapping Tool is referencing Objects but we're not copying its dll from release and local builds
    //this is because it could lead to versions incompatibilities
    //the KitManager is invoked here to load Objects in the current AppDomain for us
    try
    {
      var objects = KitManager.GetDefaultKit();
    }
    catch { }

    AvaloniaXamlLoader.Load(this);
    Name = "Speckle";
  }

  public override void OnFrameworkInitializationCompleted()
  {
    var theme = Theme.Create(Theme.Light, Primary, Accent);
    var themeBootstrap = this.LocateMaterialTheme<MaterialThemeBase>();
    themeBootstrap.CurrentTheme = theme;

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      //desktop.MainWindow = new MappingsWindow
      //{
      //  DataContext = new MappingsViewModel(),
      //};
      desktop.MainWindow = new MainWindow { DataContext = new MainViewModel() };
    //desktop.MainWindow = new Scheduler
    //{
    //  DataContext = new SchedulerViewModel(),
    //};
    //desktop.MainWindow = new Share
    //{
    //  DataContext = new ShareViewModel(),
    //};
    base.OnFrameworkInitializationCompleted();
  }
}
