using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using DesktopUI2.Models;
using DesktopUI2.Views.Pages;
using DesktopUI2.Views.Windows.Dialogs;
using Material.Styles.Themes;
using ReactiveUI;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;

namespace DesktopUI2.ViewModels;

public class MainViewModel : ViewModelBase, IScreen, IDialogHost
{
  private UserControl _dialogBody;

  public MainViewModel(ConnectorBindings _bindings)
  {
    Bindings = _bindings;
    Init();
  }

  public MainViewModel()
  {
    Init();
  }

  public string TitleFull => "Speckle for " + Bindings.GetHostAppNameVersion();

  public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

  public static RoutingState RouterInstance { get; private set; }

  public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

  public static MainViewModel Instance { get; private set; }

  public static HomeViewModel Home { get; private set; }

  public bool DialogVisible => _dialogBody != null;

  public double DialogOpacity => _dialogBody != null ? 1 : 0;

  public UserControl DialogBody
  {
    get => _dialogBody;
    set
    {
      this.RaiseAndSetIfChanged(ref _dialogBody, value);
      this.RaisePropertyChanged(nameof(DialogVisible));
      this.RaisePropertyChanged(nameof(DialogOpacity));
    }
  }

  public RoutingState Router { get; private set; }

  private void Init()
  {
    Instance = this;
    Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());

    RxApp.DefaultExceptionHandler = Observer.Create<Exception>(CatchReactiveException);
    TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

    Router = new RoutingState();

    Locator.CurrentMutable.Register(() => new StreamEditView(), typeof(IViewFor<StreamViewModel>));
    Locator.CurrentMutable.Register(() => new HomeView(), typeof(IViewFor<HomeViewModel>));
    Locator.CurrentMutable.Register(() => new OneClickView(), typeof(IViewFor<OneClickViewModel>));
    Locator.CurrentMutable.Register(() => new CollaboratorsView(), typeof(IViewFor<CollaboratorsViewModel>));
    Locator.CurrentMutable.Register(() => new SettingsView(), typeof(IViewFor<SettingsPageViewModel>));
    Locator.CurrentMutable.Register(() => new NotificationsView(), typeof(IViewFor<NotificationsViewModel>));
    Locator.CurrentMutable.Register(() => new LogInView(), typeof(IViewFor<LogInViewModel>));
    Locator.CurrentMutable.Register(() => Bindings, typeof(ConnectorBindings));

    RouterInstance = Router; // makes the router available app-wide

    var config = ConfigManager.Load();
    ChangeTheme(config.DarkTheme);

    //reusing the same view model not to lose its state
    Home = new HomeViewModel(this);
    NavigateToDefaultScreen();
  }

  public void NavigateToDefaultScreen()
  {
    var config = ConfigManager.Load();

    if (!AccountManager.GetAccounts().Any())
    {
      Router.Navigate.Execute(new LogInViewModel(this));
    }
    else if (config.OneClickMode)
    {
      Router.Navigate.Execute(new OneClickViewModel(this));
    }
    else
    {
      Home.Refresh();
      Router.Navigate.Execute(Home);
    }
  }

  //https://github.com/AvaloniaUI/Avalonia/issues/5290
  private void CatchReactiveException(Exception ex)
  {
    SpeckleLog.Logger.Fatal(ex, "Reactive exception handler observed an exception {exceptionMessage}", ex.Message);
  }

  //https://docs.avaloniaui.net/docs/getting-started/unhandledexceptions
  private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
  {
    SpeckleLog.Logger.Fatal(e.Exception, "Error in async task {error}", e.Exception.Message);
  }

  public static void GoHome()
  {
    if (RouterInstance == null)
    {
      return;
    }

    var config = ConfigManager.Load();
    if (!config.OneClickMode)
    {
      RouterInstance.Navigate.Execute(Home);
    }
  }

  public static void CloseDialog()
  {
    Instance.DialogBody = null;
  }

  internal void ChangeTheme(bool isDark)
  {
    if (Application.Current == null)
    {
      return;
    }

    var materialTheme = Application.Current.LocateMaterialTheme<MaterialThemeBase>();
    var theme = materialTheme.CurrentTheme;

    if (isDark)
    {
      theme.SetBaseTheme(Theme.Light);
    }
    else
    {
      theme.SetBaseTheme(Theme.Dark);
    }

    materialTheme.CurrentTheme = theme;
  }
}
