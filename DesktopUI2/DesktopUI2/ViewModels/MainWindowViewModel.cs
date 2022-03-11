using DesktopUI2.Views.Pages;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;
using System.Collections.Generic;
using System.Reactive;

namespace DesktopUI2.ViewModels
{
  public class MainWindowViewModel : ViewModelBase, IScreen
  {
    public RoutingState Router { get; private set; }

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string TitleFull => "Speckle for " + Bindings.GetHostAppNameVersion();
    public string Version => "v" + Bindings.ConnectorVersion;
    public MainWindowViewModel(ConnectorBindings _bindings)
    {
      Bindings = _bindings;
      Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());
      Init();
    }
    public MainWindowViewModel()
    {
      Init();
    }

    private void Init()
    {
      Router = new RoutingState();

      Locator.CurrentMutable.Register(() => new StreamEditView(), typeof(IViewFor<StreamEditViewModel>));
      Locator.CurrentMutable.Register(() => new HomeView(), typeof(IViewFor<HomeViewModel>));
      Locator.CurrentMutable.Register(() => Bindings, typeof(ConnectorBindings));

      RouterInstance = Router; // makes the router available app-wide
      Router.Navigate.Execute(new HomeViewModel(this));

      Bindings.UpdateSavedStreams = HomeViewModel.Instance.UpdateSavedStreams;

      //var theme = PaletteHelper.GetTheme();
      //theme.SetPrimaryColor(SwatchHelper.Lookup[MaterialColor.Blue600]);
      //PaletteHelper.SetTheme(theme);
    }

    #region theme
    private static PaletteHelper m_paletteHelper;
    private static PaletteHelper PaletteHelper
    {
      get
      {
        if (m_paletteHelper is null)
          m_paletteHelper = new PaletteHelper();
        return m_paletteHelper;
      }
    }

    public void ToggleDarkThemeCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Toggle Theme" } });

      var theme = PaletteHelper.GetTheme();

      if (theme.GetBaseTheme() == BaseThemeMode.Dark)
        theme.SetBaseTheme(BaseThemeMode.Light.GetBaseTheme());
      else
        theme.SetBaseTheme(BaseThemeMode.Dark.GetBaseTheme());
      PaletteHelper.SetTheme(theme);
    }


    public void RefreshCommand()
    {
      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Refresh" } });
      HomeViewModel.Instance.Init();
    }


    #endregion

  }
}
